using ClosedXML.Excel;
using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Models;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Localization;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class OrderTemplateService(IDbConnectionFactory connectionFactory, IMapper mapper, IOrderService orderService) : IOrderTemplateService
{
    private sealed class OrderTemplatePageRow : OrderTemplate { public int TotalCount { get; set; } }

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    // A caller whose own organization is SUPER_ASSOCIATE may see/edit every descendant
    // Asociado's template regardless of Warehouse/Owner — a template is just a reusable
    // list, never itself a purchase, unlike Order (where Super Asociado is fully
    // write-blocked, see OrderService.CanManageOrganizationAsync). Everyone else may only
    // see/edit their own templates (exact organization + exact owner match, never hierarchy).
    private static async Task<bool> CanAccessTemplateAsync(
        IDbConnection connection, IRequestContext context, int templateOrganizationId, int templateOwnerUserId,
        int? callerUserId, bool requireWrite)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (!context.OrganizationId.HasValue)
            return false;

        if (requireWrite && context.RoleLevel < StaffRoleLevel)
            return false;

        if (context.OrganizationTypeCode == OrganizationTypeCodes.SuperAssociate)
        {
            if (context.OrganizationId.Value == templateOrganizationId)
                return true;

            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Organization_IsInHierarchy",
                new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = templateOrganizationId },
                commandType: CommandType.StoredProcedure);

            return canAccess == 1;
        }

        return context.OrganizationId.Value == templateOrganizationId
            && callerUserId.HasValue && callerUserId.Value == templateOwnerUserId;
    }

    private static async Task<int?> ResolveOwnerUserIdAsync(IDbConnection connection, IRequestContext context)
    {
        var user = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_User_GetByToken", new { UserToken = context.EffectiveUserToken }, commandType: CommandType.StoredProcedure);
        return user?.UserId;
    }

    private static async Task<List<OrderTemplateLine>> GetLinesAsync(IDbConnection connection, int orderTemplateId)
    {
        var lines = await connection.QueryAsync<OrderTemplateLine>(
            "sp_OrderTemplateLine_GetByOrderTemplateId", new { OrderTemplateId = orderTemplateId }, commandType: CommandType.StoredProcedure);
        return lines.ToList();
    }

    private static Task<OrderTemplateLine?> GetLineByTokenAsync(IDbConnection connection, Guid orderTemplateLineToken)
    {
        return connection.QueryFirstOrDefaultAsync<OrderTemplateLine>(
            "sp_OrderTemplateLine_GetByToken", new { OrderTemplateLineToken = orderTemplateLineToken }, commandType: CommandType.StoredProcedure)!;
    }

    public async Task<PagedResult<OrderTemplateDto>> GetPagedAsync(Guid? organizationToken, Guid? warehouseToken, string? searchText, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        var isSuperAdmin = context.RoleLevel >= SuperAdminRoleLevel;
        var isSuperAssociate = context.OrganizationTypeCode == OrganizationTypeCodes.SuperAssociate;

        int? rootOrganizationId;
        if (organizationToken.HasValue)
        {
            var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                "sp_Organization_GetByToken", new { OrganizationToken = organizationToken.Value, RootOrganizationId = (int?)null }, commandType: CommandType.StoredProcedure);

            if (organization is null)
                return new PagedResult<OrderTemplateDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            var canAccessOrg = isSuperAdmin || (isSuperAssociate && context.OrganizationId.HasValue &&
                (context.OrganizationId.Value == organization.OrganizationId ||
                 await connection.ExecuteScalarAsync<int>("sp_Organization_IsInHierarchy",
                     new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = organization.OrganizationId },
                     commandType: CommandType.StoredProcedure) == 1)) ||
                (!isSuperAssociate && context.OrganizationId == organization.OrganizationId);

            if (!canAccessOrg)
                return new PagedResult<OrderTemplateDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            rootOrganizationId = organization.OrganizationId;
        }
        else if (isSuperAdmin)
        {
            rootOrganizationId = null;
        }
        else if (context.OrganizationId.HasValue)
        {
            rootOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            throw new ApiException(ErrorCodes.OrderTemplateNoOrganizationContext, "This action requires an organization-scoped account.", 403);
        }

        int? warehouseId = null;
        if (warehouseToken.HasValue)
        {
            var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
                "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken.Value }, commandType: CommandType.StoredProcedure);
            if (warehouse is null)
                return new PagedResult<OrderTemplateDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
            warehouseId = warehouse.WarehouseId;
        }

        // Only SuperAdmin/SuperAssociate may browse every owner's templates in scope —
        // everyone else is always restricted to their own, regardless of what was asked.
        int? ownerUserId = null;
        if (!isSuperAdmin && !isSuperAssociate)
        {
            ownerUserId = await ResolveOwnerUserIdAsync(connection, context);
            if (!ownerUserId.HasValue)
                return new PagedResult<OrderTemplateDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@WarehouseId", warehouseId);
        p.Add("@OwnerUserId", ownerUserId);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim());
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<OrderTemplatePageRow>(
            "sp_OrderTemplate_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<OrderTemplateDto>
        {
            Items = mapper.MapList<OrderTemplateDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<OrderTemplateDto?> GetByTokenAsync(Guid orderTemplateToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var template = await connection.QueryFirstOrDefaultAsync<OrderTemplate>(
            "sp_OrderTemplate_GetByToken", new { OrderTemplateToken = orderTemplateToken }, commandType: CommandType.StoredProcedure);

        if (template is null)
            return null;

        var callerUserId = await ResolveOwnerUserIdAsync(connection, context);
        if (!await CanAccessTemplateAsync(connection, context, template.OrganizationId, template.OwnerUserId, callerUserId, requireWrite: false))
            return null;

        var dto = mapper.Map<OrderTemplateDto>(template);
        dto.Lines = mapper.MapList<OrderTemplateLineDto>(await GetLinesAsync(connection, template.OrderTemplateId));
        return dto;
    }

    public async Task<OrderTemplateDto?> CreateAsync(Guid warehouseToken, string name, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);

        if (warehouse is null)
            return null;

        // No existing template row yet to check ownership against — a simpler direct gate:
        // SuperAdmin unrestricted; a SuperAssociate caller may create for any org in their
        // hierarchy; everyone else (Staff+) only for their own organization.
        var isSuperAdmin = context.RoleLevel >= SuperAdminRoleLevel;
        var isSuperAssociate = context.OrganizationTypeCode == OrganizationTypeCodes.SuperAssociate;

        var canCreate = isSuperAdmin
            || (context.RoleLevel >= StaffRoleLevel && context.OrganizationId.HasValue &&
                (context.OrganizationId.Value == warehouse.OrganizationId ||
                 (isSuperAssociate && await connection.ExecuteScalarAsync<int>("sp_Organization_IsInHierarchy",
                     new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = warehouse.OrganizationId },
                     commandType: CommandType.StoredProcedure) == 1)));

        if (!canCreate)
            throw new ApiException(ErrorCodes.OrderTemplateForbidden, "Cannot create a template for a warehouse outside your organization.", 403);

        var ownerUserId = await ResolveOwnerUserIdAsync(connection, context);
        if (!ownerUserId.HasValue)
            throw new ApiException(ErrorCodes.OrderTemplateNoOrganizationContext, "Could not resolve the current user.", 403);

        var p = new DynamicParameters();
        p.Add("@OrderTemplateToken", Guid.NewGuid());
        p.Add("@Name", name);
        p.Add("@OrganizationId", warehouse.OrganizationId);
        p.Add("@WarehouseId", warehouse.WarehouseId);
        p.Add("@OwnerUserId", ownerUserId.Value);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        var created = await connection.QueryFirstOrDefaultAsync<OrderTemplate>(
            "sp_OrderTemplate_Create", p, commandType: CommandType.StoredProcedure);

        return created is null ? null : mapper.Map<OrderTemplateDto>(created);
    }

    public async Task<OrderTemplateDto?> RenameAsync(Guid orderTemplateToken, string name, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var template = await connection.QueryFirstOrDefaultAsync<OrderTemplate>(
            "sp_OrderTemplate_GetByToken", new { OrderTemplateToken = orderTemplateToken }, commandType: CommandType.StoredProcedure);

        if (template is null)
            return null;

        var callerUserId = await ResolveOwnerUserIdAsync(connection, context);
        if (!await CanAccessTemplateAsync(connection, context, template.OrganizationId, template.OwnerUserId, callerUserId, requireWrite: true))
            throw new ApiException(ErrorCodes.OrderTemplateForbidden, "Cannot modify this template.", 403);

        var updated = await connection.QueryFirstOrDefaultAsync<OrderTemplate>(
            "sp_OrderTemplate_Rename",
            new { OrderTemplateToken = orderTemplateToken, Name = name, LastUpdatedBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<OrderTemplateDto>(updated);
    }

    public async Task<bool> DeleteAsync(Guid orderTemplateToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var template = await connection.QueryFirstOrDefaultAsync<OrderTemplate>(
            "sp_OrderTemplate_GetByToken", new { OrderTemplateToken = orderTemplateToken }, commandType: CommandType.StoredProcedure);

        if (template is null)
            return false;

        var callerUserId = await ResolveOwnerUserIdAsync(connection, context);
        if (!await CanAccessTemplateAsync(connection, context, template.OrganizationId, template.OwnerUserId, callerUserId, requireWrite: true))
            throw new ApiException(ErrorCodes.OrderTemplateForbidden, "Cannot delete this template.", 403);

        await connection.ExecuteAsync(
            "sp_OrderTemplate_Delete", new { OrderTemplateToken = orderTemplateToken }, commandType: CommandType.StoredProcedure);

        return true;
    }

    public async Task<OrderTemplateLineDto?> AddLineAsync(Guid orderTemplateToken, Guid articleToken, decimal quantity, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var template = await connection.QueryFirstOrDefaultAsync<OrderTemplate>(
            "sp_OrderTemplate_GetByToken", new { OrderTemplateToken = orderTemplateToken }, commandType: CommandType.StoredProcedure);

        if (template is null)
            return null;

        var callerUserId = await ResolveOwnerUserIdAsync(connection, context);
        if (!await CanAccessTemplateAsync(connection, context, template.OrganizationId, template.OwnerUserId, callerUserId, requireWrite: true))
            throw new ApiException(ErrorCodes.OrderTemplateForbidden, "Cannot modify this template.", 403);

        // Pass the Template's OWN organization, never the acting user's identity — same
        // reasoning as OrderService.AddLineAsync/ImportLinesAsync (see CLAUDE.md, "Supplier
        // global/private scoping"): a private-supplier article must resolve for its legitimate
        // owning organization regardless of who's acting (including a Super Asociado editing a
        // descendant's template), and ContextRoleLevel is deliberately left at 0 so no
        // acting-user identity can bypass the check.
        var article = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = articleToken, OrganizationId = template.OrganizationId, ContextRoleLevel = 0 }, commandType: CommandType.StoredProcedure);

        if (article is null)
            throw new ApiException(ErrorCodes.ArticleNotFound, "Article not found.", 404);

        var p = new DynamicParameters();
        p.Add("@OrderTemplateLineToken", Guid.NewGuid());
        p.Add("@OrderTemplateId", template.OrderTemplateId);
        p.Add("@ArticleId", article.ArticleId);
        p.Add("@Quantity", quantity);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        var line = await connection.QueryFirstOrDefaultAsync<OrderTemplateLine>(
            "sp_OrderTemplateLine_Upsert", p, commandType: CommandType.StoredProcedure);

        return line is null ? null : mapper.Map<OrderTemplateLineDto>(line);
    }

    public async Task<OrderTemplateLineDto?> EditLineAsync(Guid orderTemplateLineToken, decimal quantity, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existingLine = await GetLineByTokenAsync(connection, orderTemplateLineToken);
        if (existingLine is null)
            return null;

        var template = await connection.QueryFirstOrDefaultAsync<OrderTemplate>(
            "sp_OrderTemplate_GetByToken", new { OrderTemplateToken = existingLine.OrderTemplateToken }, commandType: CommandType.StoredProcedure);

        if (template is null)
            return null;

        var callerUserId = await ResolveOwnerUserIdAsync(connection, context);
        if (!await CanAccessTemplateAsync(connection, context, template.OrganizationId, template.OwnerUserId, callerUserId, requireWrite: true))
            throw new ApiException(ErrorCodes.OrderTemplateForbidden, "Cannot modify this template.", 403);

        var updated = await connection.QueryFirstOrDefaultAsync<OrderTemplateLine>(
            "sp_OrderTemplateLine_Edit",
            new { OrderTemplateLineToken = orderTemplateLineToken, Quantity = quantity, LastUpdatedBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<OrderTemplateLineDto>(updated);
    }

    public async Task<bool> DeleteLineAsync(Guid orderTemplateLineToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existingLine = await GetLineByTokenAsync(connection, orderTemplateLineToken);
        if (existingLine is null)
            return false;

        var template = await connection.QueryFirstOrDefaultAsync<OrderTemplate>(
            "sp_OrderTemplate_GetByToken", new { OrderTemplateToken = existingLine.OrderTemplateToken }, commandType: CommandType.StoredProcedure);

        if (template is null)
            return false;

        var callerUserId = await ResolveOwnerUserIdAsync(connection, context);
        if (!await CanAccessTemplateAsync(connection, context, template.OrganizationId, template.OwnerUserId, callerUserId, requireWrite: true))
            throw new ApiException(ErrorCodes.OrderTemplateForbidden, "Cannot modify this template.", 403);

        await connection.ExecuteAsync(
            "sp_OrderTemplateLine_Delete", new { OrderTemplateLineToken = orderTemplateLineToken }, commandType: CommandType.StoredProcedure);

        return true;
    }

    public async Task<(byte[] FileBytes, string FileName)> ExportAsync(Guid orderTemplateToken, string? language, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var template = await connection.QueryFirstOrDefaultAsync<OrderTemplate>(
            "sp_OrderTemplate_GetByToken", new { OrderTemplateToken = orderTemplateToken }, commandType: CommandType.StoredProcedure);

        if (template is null)
            throw new ApiException(ErrorCodes.OrderTemplateNotFound, "Order template not found.", 404);

        var callerUserId = await ResolveOwnerUserIdAsync(connection, context);
        if (!await CanAccessTemplateAsync(connection, context, template.OrganizationId, template.OwnerUserId, callerUserId, requireWrite: false))
            throw new ApiException(ErrorCodes.OrderTemplateForbidden, "Cannot export this template.", 403);

        var lines = await GetLinesAsync(connection, template.OrderTemplateId);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("OrderTemplate");

        // Column layout matches OrderService.ImportLinesAsync's expected read order exactly —
        // this export IS the import file. Price/CurrencyCode are informational only for a
        // PRODUCT article (always re-resolved live from the catalog on import regardless of
        // what's typed here) — only meaningful as real input for a SERVICE/MIXED article with
        // no catalog price, where they become the manual price. ArticleToken (hidden) is the
        // precise, typo-proof match key on re-import — same pattern as Articles/ArticlePrices.
        string[] headers = ["SupplierName", "SupplierSku", "ArticleName", "Quantity", "Price", "CurrencyCode", "ArticleToken"];
        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        worksheet.Row(1).Style.Font.Bold = true;

        var r = 2;
        foreach (var line in lines)
        {
            var priceParams = new DynamicParameters();
            priceParams.Add("@ArticleId", line.ArticleId);
            priceParams.Add("@OrganizationId", template.OrganizationId);
            priceParams.Add("@CurrencyCode", null, DbType.AnsiString, size: 10, direction: ParameterDirection.InputOutput);
            priceParams.Add("@AsOfDate", DateTime.UtcNow.Date);

            var priceRow = await connection.QueryFirstOrDefaultAsync<ArticlePrice>(
                "sp_ArticlePrice_GetCurrent", priceParams, commandType: CommandType.StoredProcedure);

            worksheet.Cell(r, 1).Value = line.SupplierName;
            worksheet.Cell(r, 2).Value = line.SupplierSku;
            worksheet.Cell(r, 3).Value = line.ArticleName;
            worksheet.Cell(r, 4).Value = line.Quantity;
            worksheet.Cell(r, 5).Value = priceRow?.Price;
            worksheet.Cell(r, 6).Value = priceRow?.CurrencyCode ?? priceParams.Get<string?>("@CurrencyCode");
            worksheet.Cell(r, 7).Value = line.ArticleToken.ToString();
            r++;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), $"order_template_{template.Name}_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public async Task<ApplyOrderTemplateResultDto?> ApplyToOrderAsync(Guid orderTemplateToken, Guid orderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var template = await connection.QueryFirstOrDefaultAsync<OrderTemplate>(
            "sp_OrderTemplate_GetByToken", new { OrderTemplateToken = orderTemplateToken }, commandType: CommandType.StoredProcedure);

        if (template is null)
            return null;

        var callerUserId = await ResolveOwnerUserIdAsync(connection, context);
        if (!await CanAccessTemplateAsync(connection, context, template.OrganizationId, template.OwnerUserId, callerUserId, requireWrite: false))
            return null;

        var lines = await GetLinesAsync(connection, template.OrderTemplateId);

        var result = new ApplyOrderTemplateResultDto
        {
            OrderTemplateToken = orderTemplateToken,
            OrderToken = orderToken,
            TotalLines = lines.Count
        };

        // Never wrapped in a transaction, tolerating partial success by design — same
        // convention as ArticleService.BulkImportArticlesAsync: each AddLineAsync call already
        // opens/commits its own connection independently, and one line's failure must never
        // block the rest from being added.
        foreach (var line in lines)
        {
            var lineResult = new ApplyOrderTemplateLineResultDto
            {
                ArticleToken = line.ArticleToken,
                ArticleName = line.ArticleName,
                SupplierName = line.SupplierName,
                SupplierType = line.SupplierType,
                Quantity = line.Quantity
            };

            try
            {
                await orderService.AddLineAsync(orderToken, line.ArticleToken, line.Quantity, null, null, context, cancellationToken);
                lineResult.Outcome = ApplyOrderTemplateLineOutcomes.Succeeded;
                result.SucceededCount++;
            }
            catch (ApiException ex) when (ex.Code == ErrorCodes.ArticlePriceManualRequired)
            {
                // Not a failure — the frontend queues this line into the existing manual-price
                // modal (the same one used for a single catalog-browse add) rather than
                // treating it as an error.
                lineResult.Outcome = ApplyOrderTemplateLineOutcomes.NeedsManualPrice;
                lineResult.ErrorCode = ex.Code;
                lineResult.ErrorMessage = ex.Message;
                result.NeedsManualPriceCount++;
            }
            catch (ApiException ex)
            {
                lineResult.Outcome = ApplyOrderTemplateLineOutcomes.Failed;
                lineResult.ErrorCode = ex.Code;
                lineResult.ErrorMessage = ex.Message;
                result.FailedCount++;
            }
            catch (Exception)
            {
                lineResult.Outcome = ApplyOrderTemplateLineOutcomes.Failed;
                lineResult.ErrorCode = ErrorCodes.UnhandledError;
                lineResult.ErrorMessage = "An unexpected error occurred while adding this line.";
                result.FailedCount++;
            }

            result.Lines.Add(lineResult);
        }

        return result;
    }
}
