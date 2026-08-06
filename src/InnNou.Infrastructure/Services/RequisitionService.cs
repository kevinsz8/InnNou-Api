using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace InnNou.Infrastructure.Services;

public class RequisitionService(IDbConnectionFactory connectionFactory, IMapper mapper, INotificationService notificationService, ILogger<RequisitionService> logger) : IRequisitionService
{
    private sealed class RequisitionPageRow : Requisition { public int TotalCount { get; set; } }

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    // Read visibility, no OrganizationTypeCode restriction — mirrors InventoryService's own copy.
    // targetWarehouseId layers WarehouseScopeGuard on top for a WarehouseContact's own login —
    // pass null for an operation with no specific target warehouse yet.
    private static async Task<bool> CanReadOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId, int? targetWarehouseId = null)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (!context.OrganizationId.HasValue)
            return false;

        if (!WarehouseScopeGuard.Allows(context, targetWarehouseId))
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    // Write visibility — only a caller whose own organization is ASSOCIATE may write; SuperAdmin
    // (no organization of their own, unless impersonating) and SUPER_ASSOCIATE are read-only —
    // requisitions happen at the property level, same reasoning as Orders/Inventory/Goods
    // Receipts. targetWarehouseId layers WarehouseScopeGuard the same way as the read variant —
    // pass null for CreateAsync (no existing Requisition/Warehouse to match yet), which correctly
    // blocks a warehouse-scoped caller from creating a Requisition for a sibling warehouse.
    private static async Task<bool> CanManageOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId, int? targetWarehouseId = null)
    {
        if (context.OrganizationTypeCode != OrganizationTypeCodes.Associate)
            return false;

        if (context.RoleLevel < StaffRoleLevel || !context.OrganizationId.HasValue)
            return false;

        if (!WarehouseScopeGuard.Allows(context, targetWarehouseId))
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    private static async Task<List<RequisitionLine>> GetLinesAsync(IDbConnection connection, int requisitionId)
    {
        var lines = await connection.QueryAsync<RequisitionLine>(
            "sp_RequisitionLine_GetByRequisitionId", new { RequisitionId = requisitionId }, commandType: CommandType.StoredProcedure);
        return lines.ToList();
    }

    private readonly record struct ResolvedArticleQuantity(decimal Normalized, int? UnitId, decimal? RawQuantity);

    // Resolves a quantity entered against enteredUnitId (or directly against the article's own
    // PurchaseUnitId when unitToken is null) to a PurchaseUnitId-normalized value, per
    // ArticleUnitConversion. Used by every Requisition write path that accepts a quantity
    // (Create/AddLine/EditLine/CreateIssue) — see .claude/RequisitionsModule.md's "unit-aware
    // quantities" section for the full design.
    private static async Task<ResolvedArticleQuantity> ResolveArticleQuantityAsync(
        IDbConnection connection, IDbTransaction? transaction, IMapper mapper,
        int articleId, int purchaseUnitId, string articleName, Guid? unitToken, decimal enteredQuantity)
    {
        if (unitToken is null)
            return new ResolvedArticleQuantity(enteredQuantity, null, null);

        var unit = await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
            "sp_UnitOfMeasure_GetByToken", new { UnitOfMeasureToken = unitToken.Value }, transaction, commandType: CommandType.StoredProcedure);
        if (unit is null)
            throw new ApiException(ErrorCodes.ArticleUnitNotValidForArticle, $"Unit of measure not found for '{articleName}'.", 404);

        if (unit.UnitOfMeasureId == purchaseUnitId)
            return new ResolvedArticleQuantity(enteredQuantity, null, null);

        var levels = mapper.MapList<ArticlePackagingLevelDto>(
            await connection.QueryAsync<ArticlePackagingLevel>(
                "sp_ArticlePackagingLevel_GetByArticleId", new { ArticleId = articleId }, transaction, commandType: CommandType.StoredProcedure));

        var normalized = ArticleUnitConversion.ToPurchaseUnitQuantity(purchaseUnitId, levels, unit.UnitOfMeasureId, enteredQuantity);
        if (normalized is null)
            throw new ApiException(ErrorCodes.ArticleUnitNotValidForArticle, $"'{unit.Code}' is not a valid unit for '{articleName}'.", 400);

        return new ResolvedArticleQuantity(normalized.Value, unit.UnitOfMeasureId, enteredQuantity);
    }

    // Best-effort/non-blocking (same convention as every other notification call site). Recipient
    // is resolved directly from the Requisition's own CreatedBy — unlike PurchaseOrderService's
    // NotifyOrderBuyerAsync (which has to look up a separate Order), Requisition IS the entity the
    // requesting Department created, so no extra round trip is needed.
    private async Task NotifyRequisitionCreatorAsync(DbConnection connection, Requisition requisition, NotificationType type, object data, IRequestContext context, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(requisition.CreatedBy, out var creatorToken))
            return;

        try
        {
            // notificationService.NotifyAsync opens its own connection — closing this one first
            // keeps at most one connection open at a time on this logical unit of work (Dapper
            // transparently reopens it on the caller's next query). See
            // PurchaseOrderService.NotifyOrderBuyerAsync's own comment for the full reasoning.
            await connection.CloseAsync();

            await notificationService.NotifyAsync(creatorToken, type, data, $"/requisitions/{requisition.RequisitionToken}", context, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notification failed for Requisition {RequisitionToken}", requisition.RequisitionToken);
        }
    }

    public async Task<RequisitionDto?> CreateAsync(Guid warehouseToken, Guid departmentToken, string? notes, List<CreateRequisitionLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);
        if (warehouse is null)
            throw new ApiException(ErrorCodes.RequisitionWarehouseNotFound, "Warehouse not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, warehouse.OrganizationId, warehouse.WarehouseId))
            throw new ApiException(ErrorCodes.RequisitionForbidden, "Cannot create a requisition for a warehouse outside your scope.", 403);

        if (!warehouse.CanIssueToDepartment)
            throw new ApiException(ErrorCodes.RequisitionWarehouseCannotIssue, "This warehouse is not configured to issue stock to departments.", 400);

        var department = await connection.QueryFirstOrDefaultAsync<Department>(
            "sp_Department_GetByToken", new { DepartmentToken = departmentToken }, commandType: CommandType.StoredProcedure);
        if (department is null || !department.IsActive)
            throw new ApiException(ErrorCodes.RequisitionDepartmentNotFound, "Department not found.", 404);

        if (department.OrganizationId != warehouse.OrganizationId)
            throw new ApiException(ErrorCodes.RequisitionDepartmentOrganizationMismatch, "The department must belong to the same organization as the warehouse.", 400);

        if (lines.Count == 0)
            throw new ApiException(ErrorCodes.RequisitionEmpty, "At least one line must be requested.", 400);

        var validatedLines = new List<(Article Article, ResolvedArticleQuantity Quantity, string? Notes)>();
        var seenArticleIds = new HashSet<int>();
        foreach (var input in lines)
        {
            if (input.QuantityRequested <= 0)
                throw new ApiException(ErrorCodes.RequisitionInvalidQuantity, "Requested quantity must be greater than zero.", 400);

            var article = await connection.QueryFirstOrDefaultAsync<Article>(
                "sp_Article_GetByToken", new { ArticleToken = input.ArticleToken }, commandType: CommandType.StoredProcedure);
            if (article is null)
                throw new ApiException(ErrorCodes.RequisitionArticleNotFound, $"Article '{input.ArticleToken}' not found.", 404);

            if (!seenArticleIds.Add(article.ArticleId))
                throw new ApiException(ErrorCodes.RequisitionInvalidQuantity, $"Article '{article.Name}' was submitted more than once.", 400);

            var resolvedQuantity = await ResolveArticleQuantityAsync(
                connection, null, mapper, article.ArticleId, article.PurchaseUnitId, article.Name, input.UnitToken, input.QuantityRequested);

            validatedLines.Add((article, resolvedQuantity, input.Notes));
        }

        var actor = context.ActorUserToken.ToString();
        Guid createdRequisitionToken;

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var headerParams = new DynamicParameters();
            headerParams.Add("@RequisitionToken", Guid.NewGuid());
            headerParams.Add("@OrganizationId", warehouse.OrganizationId);
            headerParams.Add("@WarehouseId", warehouse.WarehouseId);
            headerParams.Add("@DepartmentId", department.DepartmentId);
            headerParams.Add("@Notes", notes);
            headerParams.Add("@CreatedBy", actor);

            var header = await connection.QueryFirstOrDefaultAsync<Requisition>(
                "sp_Requisition_Create", headerParams, transaction, commandType: CommandType.StoredProcedure)
                ?? throw new InvalidOperationException("sp_Requisition_Create returned no row for a Warehouse/Department already validated above.");

            createdRequisitionToken = header.RequisitionToken;

            foreach (var (article, quantity, lineNotes) in validatedLines)
            {
                var lineParams = new DynamicParameters();
                lineParams.Add("@RequisitionLineToken", Guid.NewGuid());
                lineParams.Add("@RequisitionId", header.RequisitionId);
                lineParams.Add("@ArticleId", article.ArticleId);
                lineParams.Add("@QuantityRequested", quantity.Normalized);
                lineParams.Add("@RequestedUnitId", quantity.UnitId);
                lineParams.Add("@RequestedQuantity", quantity.RawQuantity);
                lineParams.Add("@Notes", lineNotes);
                lineParams.Add("@CreatedBy", actor);

                await connection.ExecuteAsync("sp_RequisitionLine_Create", lineParams, transaction, commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        // GetByTokenAsync below opens its own connection — dispose this transaction and close
        // this connection first so at most one connection is ever open at a time on this logical
        // unit of work (Dapper reopens transparently). Same reasoning as
        // PurchaseOrderService.NotifyOrderBuyerAsync's own explicit connection.CloseAsync().
        await transaction.DisposeAsync();
        await connection.CloseAsync();

        return await GetByTokenAsync(createdRequisitionToken, context, cancellationToken);
    }

    public async Task<RequisitionLineDto?> AddLineAsync(Guid requisitionToken, Guid articleToken, decimal quantityRequested, Guid? unitToken, string? notes, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var requisition = await connection.QueryFirstOrDefaultAsync<Requisition>(
            "sp_Requisition_GetByToken", new { RequisitionToken = requisitionToken }, commandType: CommandType.StoredProcedure);
        if (requisition is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, requisition.OrganizationId, requisition.WarehouseId))
            throw new ApiException(ErrorCodes.RequisitionForbidden, "Cannot modify a requisition outside your scope.", 403);

        if (requisition.Status != RequisitionStatus.Requested)
            throw new ApiException(ErrorCodes.RequisitionNotEditable, "Only a requisition still pending decision can be modified.", 409);

        if (quantityRequested <= 0)
            throw new ApiException(ErrorCodes.RequisitionInvalidQuantity, "Requested quantity must be greater than zero.", 400);

        var article = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = articleToken }, commandType: CommandType.StoredProcedure);
        if (article is null)
            throw new ApiException(ErrorCodes.RequisitionArticleNotFound, "Article not found.", 404);

        var resolvedQuantity = await ResolveArticleQuantityAsync(
            connection, null, mapper, article.ArticleId, article.PurchaseUnitId, article.Name, unitToken, quantityRequested);

        var p = new DynamicParameters();
        p.Add("@RequisitionLineToken", Guid.NewGuid());
        p.Add("@RequisitionId", requisition.RequisitionId);
        p.Add("@ArticleId", article.ArticleId);
        p.Add("@QuantityRequested", resolvedQuantity.Normalized);
        p.Add("@RequestedUnitId", resolvedQuantity.UnitId);
        p.Add("@RequestedQuantity", resolvedQuantity.RawQuantity);
        p.Add("@Notes", notes);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        var line = await connection.QueryFirstOrDefaultAsync<RequisitionLine>(
            "sp_RequisitionLine_Create", p, commandType: CommandType.StoredProcedure);

        return line is null ? null : mapper.Map<RequisitionLineDto>(line);
    }

    public async Task<RequisitionLineDto?> EditLineAsync(Guid requisitionLineToken, decimal quantityRequested, Guid? unitToken, string? notes, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existingLine = await connection.QueryFirstOrDefaultAsync<RequisitionLine>(
            "sp_RequisitionLine_GetByToken", new { RequisitionLineToken = requisitionLineToken }, commandType: CommandType.StoredProcedure);
        if (existingLine is null)
            return null;

        var requisition = await connection.QueryFirstOrDefaultAsync<Requisition>(
            "sp_Requisition_GetByToken", new { RequisitionToken = existingLine.RequisitionToken }, commandType: CommandType.StoredProcedure);
        if (requisition is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, requisition.OrganizationId, requisition.WarehouseId))
            throw new ApiException(ErrorCodes.RequisitionForbidden, "Cannot modify a requisition outside your scope.", 403);

        if (requisition.Status != RequisitionStatus.Requested)
            throw new ApiException(ErrorCodes.RequisitionNotEditable, "Only a requisition still pending decision can be modified.", 409);

        if (quantityRequested <= 0)
            throw new ApiException(ErrorCodes.RequisitionInvalidQuantity, "Requested quantity must be greater than zero.", 400);

        var article = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = existingLine.ArticleToken }, commandType: CommandType.StoredProcedure);
        if (article is null)
            throw new ApiException(ErrorCodes.RequisitionArticleNotFound, "Article not found.", 404);

        var resolvedQuantity = await ResolveArticleQuantityAsync(
            connection, null, mapper, article.ArticleId, article.PurchaseUnitId, article.Name, unitToken, quantityRequested);

        var p = new DynamicParameters();
        p.Add("@RequisitionLineToken", requisitionLineToken);
        p.Add("@QuantityRequested", resolvedQuantity.Normalized);
        p.Add("@RequestedUnitId", resolvedQuantity.UnitId);
        p.Add("@RequestedQuantity", resolvedQuantity.RawQuantity);
        p.Add("@Notes", notes);

        var updated = await connection.QueryFirstOrDefaultAsync<RequisitionLine>(
            "sp_RequisitionLine_Edit", p, commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<RequisitionLineDto>(updated);
    }

    public async Task<bool> DeleteLineAsync(Guid requisitionLineToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existingLine = await connection.QueryFirstOrDefaultAsync<RequisitionLine>(
            "sp_RequisitionLine_GetByToken", new { RequisitionLineToken = requisitionLineToken }, commandType: CommandType.StoredProcedure);
        if (existingLine is null)
            return false;

        var requisition = await connection.QueryFirstOrDefaultAsync<Requisition>(
            "sp_Requisition_GetByToken", new { RequisitionToken = existingLine.RequisitionToken }, commandType: CommandType.StoredProcedure);
        if (requisition is null)
            return false;

        if (!await CanManageOrganizationAsync(connection, context, requisition.OrganizationId, requisition.WarehouseId))
            throw new ApiException(ErrorCodes.RequisitionForbidden, "Cannot modify a requisition outside your scope.", 403);

        if (requisition.Status != RequisitionStatus.Requested)
            throw new ApiException(ErrorCodes.RequisitionNotEditable, "Only a requisition still pending decision can be modified.", 409);

        await connection.ExecuteAsync(
            "sp_RequisitionLine_Delete", new { RequisitionLineToken = requisitionLineToken }, commandType: CommandType.StoredProcedure);

        return true;
    }

    public async Task<RequisitionDto?> GetByTokenAsync(Guid requisitionToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var requisition = await connection.QueryFirstOrDefaultAsync<Requisition>(
            "sp_Requisition_GetByToken", new { RequisitionToken = requisitionToken }, commandType: CommandType.StoredProcedure);

        if (requisition is null || !await CanReadOrganizationAsync(connection, context, requisition.OrganizationId, requisition.WarehouseId))
            return null;

        var dto = mapper.Map<RequisitionDto>(requisition);
        var lineEntities = await GetLinesAsync(connection, requisition.RequisitionId);
        var lineDtos = mapper.MapList<RequisitionLineDto>(lineEntities);
        dto.Lines = lineDtos;
        dto.LineCount = lineDtos.Count;

        var issues = (await connection.QueryAsync<RequisitionIssue>(
            "sp_RequisitionIssue_GetByRequisitionId", new { requisition.RequisitionId }, commandType: CommandType.StoredProcedure)).ToList();

        var issueLineEntitiesByIssue = new List<(RequisitionIssue Issue, List<RequisitionIssueLine> Lines)>();
        foreach (var issue in issues)
        {
            var issueLineEntities = (await connection.QueryAsync<RequisitionIssueLine>(
                "sp_RequisitionIssueLine_GetByRequisitionIssueId", new { issue.RequisitionIssueId }, commandType: CommandType.StoredProcedure)).ToList();
            issueLineEntitiesByIssue.Add((issue, issueLineEntities));
        }

        // Batched across both line types in one round trip — same anti-N+1 shape as
        // ExportArticlesAsync/GetPackagingConversionReportAsync — so the "how much is that in
        // the article's own Unidad Definida" secondary reference (ArticleUnitConversion.
        // GetDefinedUnitEquivalent) costs nothing extra per line.
        var allArticleIds = lineEntities.Select(l => l.ArticleId)
            .Concat(issueLineEntitiesByIssue.SelectMany(x => x.Lines.Select(l => l.ArticleId)))
            .Distinct().ToList();
        var levelsByArticleId = new Dictionary<int, List<ArticlePackagingLevelDto>>();
        if (allArticleIds.Count > 0)
        {
            var levelRows = await connection.QueryAsync<ArticlePackagingLevel>(
                "sp_ArticlePackagingLevel_GetByArticleIds", new { ArticleIds = string.Join(',', allArticleIds) }, commandType: CommandType.StoredProcedure);
            levelsByArticleId = levelRows.GroupBy(l => l.ArticleId)
                .ToDictionary(g => g.Key, g => mapper.MapList<ArticlePackagingLevelDto>(g.ToList()));
        }

        for (var i = 0; i < lineEntities.Count; i++)
        {
            var entity = lineEntities[i];
            var levels = levelsByArticleId.GetValueOrDefault(entity.ArticleId, []);
            var effectiveUnitId = entity.RequestedUnitId ?? entity.PurchaseUnitId;
            var effectiveQuantity = entity.RequestedQuantity ?? entity.QuantityRequested;
            var equivalent = ArticleUnitConversion.GetDefinedUnitEquivalent(entity.PurchaseUnitId, levels, effectiveUnitId, effectiveQuantity);
            if (equivalent is not null)
            {
                lineDtos[i].DefinedUnitCode = equivalent.Value.Code;
                lineDtos[i].DefinedUnitNameTranslations = equivalent.Value.NameTranslations;
                lineDtos[i].DefinedUnitQuantity = equivalent.Value.Quantity;
            }
        }

        foreach (var (issue, issueLineEntities) in issueLineEntitiesByIssue)
        {
            var issueDto = mapper.Map<RequisitionIssueDto>(issue);
            var issueLineDtos = mapper.MapList<RequisitionIssueLineDto>(issueLineEntities);

            for (var i = 0; i < issueLineEntities.Count; i++)
            {
                var entity = issueLineEntities[i];
                var levels = levelsByArticleId.GetValueOrDefault(entity.ArticleId, []);
                var effectiveUnitId = entity.IssuedUnitId ?? entity.PurchaseUnitId;
                var effectiveQuantity = entity.IssuedQuantity ?? entity.QuantityIssued;
                var equivalent = ArticleUnitConversion.GetDefinedUnitEquivalent(entity.PurchaseUnitId, levels, effectiveUnitId, effectiveQuantity);
                if (equivalent is not null)
                {
                    issueLineDtos[i].DefinedUnitCode = equivalent.Value.Code;
                    issueLineDtos[i].DefinedUnitNameTranslations = equivalent.Value.NameTranslations;
                    issueLineDtos[i].DefinedUnitQuantity = equivalent.Value.Quantity;
                }
            }

            issueDto.Lines = issueLineDtos;
            dto.Issues.Add(issueDto);
        }

        return dto;
    }

    public async Task<PagedResult<RequisitionDto>> GetPagedAsync(Guid? organizationToken, Guid? warehouseToken, Guid? departmentToken, string? status, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        int? rootOrganizationId;
        if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            rootOrganizationId = null;
        }
        else if (context.RoleLevel >= StaffRoleLevel && context.OrganizationId.HasValue)
        {
            rootOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            return new PagedResult<RequisitionDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        if (organizationToken.HasValue)
        {
            var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                "sp_Organization_GetByToken", new { OrganizationToken = organizationToken.Value, RootOrganizationId = (int?)null }, commandType: CommandType.StoredProcedure);

            if (organization is null || !await CanReadOrganizationAsync(connection, context, organization.OrganizationId))
                return new PagedResult<RequisitionDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            rootOrganizationId = organization.OrganizationId;
        }

        // Defaults to the caller's own WarehouseId (WarehouseContact login) so an unfiltered
        // request never falls through to "every warehouse in the org" — an explicit warehouseToken
        // is still validated against it below.
        int? warehouseId = context.WarehouseId;
        if (warehouseToken.HasValue)
        {
            var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
                "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken.Value }, commandType: CommandType.StoredProcedure);

            if (warehouse is null || !WarehouseScopeGuard.Allows(context, warehouse.WarehouseId))
                return new PagedResult<RequisitionDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            warehouseId = warehouse.WarehouseId;
        }

        int? departmentId = null;
        if (departmentToken.HasValue)
        {
            var department = await connection.QueryFirstOrDefaultAsync<Department>(
                "sp_Department_GetByToken", new { DepartmentToken = departmentToken.Value }, commandType: CommandType.StoredProcedure);

            if (department is null)
                return new PagedResult<RequisitionDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            departmentId = department.DepartmentId;
        }

        int? statusId = null;
        if (status is not null)
        {
            if (!RequisitionStatusCodes.TryFromCode(status, out var parsedStatus))
                return new PagedResult<RequisitionDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
            statusId = (int)parsedStatus;
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@WarehouseId", warehouseId);
        p.Add("@DepartmentId", departmentId);
        p.Add("@StatusId", statusId);
        p.Add("@FromDate", fromDate?.Date);
        p.Add("@ToDate", toDate?.Date);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<RequisitionPageRow>(
            "sp_Requisition_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<RequisitionDto>
        {
            Items = mapper.MapList<RequisitionDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<RequisitionDto?> ApproveAsync(Guid requisitionToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Requisition>(
            "sp_Requisition_GetByToken", new { RequisitionToken = requisitionToken }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId, existing.WarehouseId))
            throw new ApiException(ErrorCodes.RequisitionForbidden, "Cannot approve a requisition outside your scope.", 403);

        if (existing.Status != RequisitionStatus.Requested)
            throw new ApiException(ErrorCodes.RequisitionNotApprovable, "Only a requisition still pending decision can be approved.", 409);

        var updated = await connection.QueryFirstOrDefaultAsync<Requisition>(
            "sp_Requisition_Approve",
            new { RequisitionToken = requisitionToken, ApprovedBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        if (updated is null)
            return null;

        await NotifyRequisitionCreatorAsync(
            connection, updated, NotificationType.Requisition_Approved,
            new { requisitionNumber = updated.RequisitionNumber },
            context, cancellationToken);

        return mapper.Map<RequisitionDto>(updated);
    }

    public async Task<RequisitionDto?> RejectAsync(Guid requisitionToken, string reason, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Requisition>(
            "sp_Requisition_GetByToken", new { RequisitionToken = requisitionToken }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId, existing.WarehouseId))
            throw new ApiException(ErrorCodes.RequisitionForbidden, "Cannot reject a requisition outside your scope.", 403);

        if (existing.Status != RequisitionStatus.Requested)
            throw new ApiException(ErrorCodes.RequisitionNotRejectable, "Only a requisition still pending decision can be rejected.", 409);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ApiException(ErrorCodes.RequisitionRejectReasonRequired, "A reason is required to reject a requisition.", 400);

        var updated = await connection.QueryFirstOrDefaultAsync<Requisition>(
            "sp_Requisition_Reject",
            new { RequisitionToken = requisitionToken, RejectedBy = context.ActorUserToken.ToString(), RejectedReason = reason.Trim() },
            commandType: CommandType.StoredProcedure);

        if (updated is null)
            return null;

        await NotifyRequisitionCreatorAsync(
            connection, updated, NotificationType.Requisition_Rejected,
            new { requisitionNumber = updated.RequisitionNumber, reason = updated.RejectedReason },
            context, cancellationToken);

        return mapper.Map<RequisitionDto>(updated);
    }

    public async Task<RequisitionDto?> CancelAsync(Guid requisitionToken, string? reason, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Requisition>(
            "sp_Requisition_GetByToken", new { RequisitionToken = requisitionToken }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId, existing.WarehouseId))
            throw new ApiException(ErrorCodes.RequisitionForbidden, "Cannot cancel a requisition outside your scope.", 403);

        if (existing.Status is not (RequisitionStatus.Requested or RequisitionStatus.Approved))
            throw new ApiException(ErrorCodes.RequisitionNotCancellable, "Only a requisition with nothing issued yet can be cancelled — close it as short instead.", 409);

        var updated = await connection.QueryFirstOrDefaultAsync<Requisition>(
            "sp_Requisition_Cancel",
            new { RequisitionToken = requisitionToken, CancelledBy = context.ActorUserToken.ToString(), CancelledReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim() },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<RequisitionDto>(updated);
    }

    public async Task<RequisitionDto?> CloseShortAsync(Guid requisitionToken, string reason, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Requisition>(
            "sp_Requisition_GetByToken", new { RequisitionToken = requisitionToken }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId, existing.WarehouseId))
            throw new ApiException(ErrorCodes.RequisitionForbidden, "Cannot close a requisition outside your scope.", 403);

        if (existing.Status != RequisitionStatus.Partially_Issued)
            throw new ApiException(ErrorCodes.RequisitionNotCloseableShort, "Only a partially issued requisition can be closed as short.", 409);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ApiException(ErrorCodes.RequisitionCloseShortReasonRequired, "A reason is required to close a requisition as short.", 400);

        var updated = await connection.QueryFirstOrDefaultAsync<Requisition>(
            "sp_Requisition_CloseShort",
            new { RequisitionToken = requisitionToken, ClosedShortBy = context.ActorUserToken.ToString(), ClosedShortReason = reason.Trim() },
            commandType: CommandType.StoredProcedure);

        if (updated is null)
            return null;

        await NotifyRequisitionCreatorAsync(
            connection, updated, NotificationType.Requisition_Closed_Short,
            new { requisitionNumber = updated.RequisitionNumber, reason = updated.ClosedShortReason },
            context, cancellationToken);

        return mapper.Map<RequisitionDto>(updated);
    }

    public async Task<RequisitionIssueDto?> CreateIssueAsync(Guid requisitionToken, string? notes, List<CreateRequisitionIssueLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var requisition = await connection.QueryFirstOrDefaultAsync<Requisition>(
            "sp_Requisition_GetByToken", new { RequisitionToken = requisitionToken }, commandType: CommandType.StoredProcedure);
        if (requisition is null)
            throw new ApiException(ErrorCodes.RequisitionNotFound, "Requisition not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, requisition.OrganizationId, requisition.WarehouseId))
            throw new ApiException(ErrorCodes.RequisitionForbidden, "Cannot issue stock for a requisition outside your scope.", 403);

        if (requisition.Status is not (RequisitionStatus.Approved or RequisitionStatus.Partially_Issued))
            throw new ApiException(ErrorCodes.RequisitionNotIssuable, "Only an approved or partially issued requisition can receive an issuance.", 409);

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { requisition.WarehouseToken }, commandType: CommandType.StoredProcedure);
        if (warehouse is null || !warehouse.CanIssueToDepartment)
            throw new ApiException(ErrorCodes.RequisitionWarehouseCannotIssue, "This warehouse is not configured to issue stock to departments.", 400);

        if (lines.Count == 0)
            throw new ApiException(ErrorCodes.RequisitionIssueEmpty, "At least one line must be issued.", 400);

        var requisitionLines = (await GetLinesAsync(connection, requisition.RequisitionId))
            .ToDictionary(l => l.RequisitionLineToken);

        var stockByArticle = (await connection.QueryAsync<StockLevel>(
            "sp_StockLevel_GetAllByWarehouseId", new { warehouse.WarehouseId }, commandType: CommandType.StoredProcedure))
            .ToDictionary(s => s.ArticleId, s => s.QuantityOnHand);

        var validatedLines = new List<(RequisitionLine Line, ResolvedArticleQuantity Quantity, string? Notes)>();
        var seenLineIds = new HashSet<int>();

        foreach (var input in lines)
        {
            if (!requisitionLines.TryGetValue(input.RequisitionLineToken, out var line))
                throw new ApiException(ErrorCodes.RequisitionIssueLineNotFound, $"Line '{input.RequisitionLineToken}' does not belong to this requisition.", 404);

            if (!seenLineIds.Add(line.RequisitionLineId))
                throw new ApiException(ErrorCodes.RequisitionIssueDuplicateLine, $"Article '{line.ArticleName}' was submitted more than once.", 400);

            if (input.QuantityIssued <= 0)
                throw new ApiException(ErrorCodes.RequisitionInvalidQuantity, $"Issued quantity for '{line.ArticleName}' must be greater than zero.", 400);

            var resolvedQuantity = await ResolveArticleQuantityAsync(
                connection, null, mapper, line.ArticleId, line.PurchaseUnitId, line.ArticleName ?? line.ArticleToken.ToString(), input.UnitToken, input.QuantityIssued);

            var remaining = line.QuantityRequested - line.QuantityIssued;
            if (resolvedQuantity.Normalized > remaining)
                throw new ApiException(ErrorCodes.RequisitionOverIssueNotAllowed, $"Cannot issue {resolvedQuantity.Normalized} of '{line.ArticleName}' — only {remaining} still outstanding on this requisition.", 400);

            var currentStock = stockByArticle.GetValueOrDefault(line.ArticleId, 0m);
            if (currentStock - resolvedQuantity.Normalized < 0)
                throw new ApiException(ErrorCodes.RequisitionInsufficientStock, $"Cannot issue {resolvedQuantity.Normalized} of '{line.ArticleName}' — only {currentStock} available at this warehouse.", 400);

            // Two lines could reference the same Article — keep the running balance consistent
            // across the whole batch, same reasoning InventoryService.CreateTransferAsync applies.
            stockByArticle[line.ArticleId] = currentStock - resolvedQuantity.Normalized;

            validatedLines.Add((line, resolvedQuantity, input.Notes));
        }

        var actor = context.ActorUserToken.ToString();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var issueHeaderParams = new DynamicParameters();
            issueHeaderParams.Add("@RequisitionIssueToken", Guid.NewGuid());
            issueHeaderParams.Add("@RequisitionId", requisition.RequisitionId);
            issueHeaderParams.Add("@Notes", notes);
            issueHeaderParams.Add("@CreatedBy", actor);

            var issueHeader = await connection.QueryFirstOrDefaultAsync<RequisitionIssue>(
                "sp_RequisitionIssue_Create", issueHeaderParams, transaction, commandType: CommandType.StoredProcedure)
                ?? throw new InvalidOperationException("sp_RequisitionIssue_Create returned no row for a Requisition already validated above.");

            foreach (var (line, quantity, lineNotes) in validatedLines)
            {
                var issueLineParams = new DynamicParameters();
                issueLineParams.Add("@RequisitionIssueLineToken", Guid.NewGuid());
                issueLineParams.Add("@RequisitionIssueId", issueHeader.RequisitionIssueId);
                issueLineParams.Add("@RequisitionLineId", line.RequisitionLineId);
                issueLineParams.Add("@QuantityIssued", quantity.Normalized);
                issueLineParams.Add("@IssuedUnitId", quantity.UnitId);
                issueLineParams.Add("@IssuedQuantity", quantity.RawQuantity);
                issueLineParams.Add("@Notes", lineNotes);
                issueLineParams.Add("@CreatedBy", actor);

                var issueLine = await connection.QueryFirstOrDefaultAsync<RequisitionIssueLine>(
                    "sp_RequisitionIssueLine_Create", issueLineParams, transaction, commandType: CommandType.StoredProcedure)
                    ?? throw new InvalidOperationException("sp_RequisitionIssueLine_Create returned no row.");

                await connection.ExecuteAsync(
                    "sp_StockLevel_ApplyDelta",
                    new { warehouse.WarehouseId, line.ArticleId, Delta = -quantity.Normalized, ActorBy = actor },
                    transaction, commandType: CommandType.StoredProcedure);

                var movementParams = new DynamicParameters();
                movementParams.Add("@InventoryMovementToken", Guid.NewGuid());
                movementParams.Add("@WarehouseId", warehouse.WarehouseId);
                movementParams.Add("@ArticleId", line.ArticleId);
                movementParams.Add("@Type", InventoryMovementTypeCodes.Consumption);
                movementParams.Add("@Quantity", -quantity.Normalized);
                // Copies the issue line's own entered unit/quantity forward for full audit-trail
                // fidelity — the sign is flipped to match Quantity's own negative-for-consumption
                // convention (RawQuantity itself is always positive, what the user actually typed).
                movementParams.Add("@EnteredUnitId", quantity.UnitId);
                movementParams.Add("@EnteredQuantity", quantity.RawQuantity.HasValue ? -quantity.RawQuantity.Value : (decimal?)null);
                movementParams.Add("@RequisitionIssueLineId", issueLine.RequisitionIssueLineId);
                movementParams.Add("@CreatedBy", actor);

                await connection.ExecuteAsync("sp_InventoryMovement_Create", movementParams, transaction, commandType: CommandType.StoredProcedure);
            }

            var issuedByLineId = validatedLines.ToDictionary(v => v.Line.RequisitionLineId, v => v.Quantity.Normalized);
            var everyLineFullyIssued = requisitionLines.Values
                .All(l => l.QuantityIssued + issuedByLineId.GetValueOrDefault(l.RequisitionLineId) >= l.QuantityRequested);

            var newStatus = everyLineFullyIssued
                ? RequisitionStatusCodes.Issued
                : RequisitionStatusCodes.PartiallyIssued;

            await connection.ExecuteAsync(
                "sp_Requisition_SetStatus",
                new { RequisitionToken = requisitionToken, Status = newStatus },
                transaction, commandType: CommandType.StoredProcedure);

            await transaction.CommitAsync(cancellationToken);

            // NotifyRequisitionCreatorAsync below closes/reopens this connection — the committed
            // transaction must be disposed first, or SqlClient throws "The transaction associated
            // with the current connection has completed but has not been disposed" on that
            // Close(), silently swallowed by the notify helper's own try/catch, leaving the pooled
            // connection broken for whichever test/request reuses it next. See
            // PurchaseOrderService.CreateGoodsReceiptAsync's identical comment for the full story.
            await transaction.DisposeAsync();

            var updatedRequisition = await connection.QueryFirstOrDefaultAsync<Requisition>(
                "sp_Requisition_GetByToken", new { RequisitionToken = requisitionToken }, commandType: CommandType.StoredProcedure);
            if (updatedRequisition is not null)
            {
                await NotifyRequisitionCreatorAsync(
                    connection, updatedRequisition, NotificationType.Requisition_Issued,
                    new { requisitionNumber = updatedRequisition.RequisitionNumber },
                    context, cancellationToken);
            }

            var dto = mapper.Map<RequisitionIssueDto>(issueHeader);
            dto.Lines = mapper.MapList<RequisitionIssueLineDto>(
                await connection.QueryAsync<RequisitionIssueLine>(
                    "sp_RequisitionIssueLine_GetByRequisitionIssueId", new { issueHeader.RequisitionIssueId }, commandType: CommandType.StoredProcedure));
            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
