using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class InventoryService(IDbConnectionFactory connectionFactory, IMapper mapper) : IInventoryService
{
    private sealed class StockLevelPageRow : StockLevel { public int TotalCount { get; set; } }
    private sealed class InventoryMovementPageRow : InventoryMovement { public int TotalCount { get; set; } }
    private sealed class InventoryTransferPageRow : InventoryTransfer { public int TotalCount { get; set; } }

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    // Read visibility, no OrganizationTypeCode restriction — mirrors
    // PurchaseOrderService/OrderService.CanReadOrganizationAsync.
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

    // Write visibility — mirrors PurchaseOrderService/OrderService.CanManageOrganizationAsync:
    // only a caller whose own organization is ASSOCIATE may write; SuperAdmin (no organization of
    // their own, unless impersonating) and SUPER_ASSOCIATE are read-only — inventory operations
    // happen at the property level, same reasoning as Orders/Goods Receipts.
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

    // A warehouse mid-count (IN_PROGRESS/PRE_CLOSED — see IInventoryPeriodService) blocks
    // discretionary Adjustments/Transfers so the physical count isn't invalidated by stock moving
    // under the counters' feet. OPEN does not block (counting hasn't started yet); Goods Receipts
    // are deliberately exempted (a physical delivery can't be turned away) — see
    // .claude/InventoryPeriodsModule.md's "Design decisions" section for the tradeoff.
    private static async Task EnsureNoActiveCountInProgressAsync(IDbConnection connection, int warehouseId, string warehouseLabel)
    {
        var active = await connection.QueryFirstOrDefaultAsync<InventoryPeriod>(
            "sp_InventoryPeriod_GetActiveByWarehouseId", new { WarehouseId = warehouseId }, commandType: CommandType.StoredProcedure);

        if (active is not null && active.Status is InventoryPeriodStatus.In_Progress or InventoryPeriodStatus.Pre_Closed)
            throw new ApiException(ErrorCodes.InventoryWarehouseCountInProgress, $"{warehouseLabel} has an inventory count in progress — adjustments and transfers are blocked until it closes.", 409);
    }

    private readonly record struct ResolvedArticleQuantity(decimal Normalized, int? UnitId, decimal? RawQuantity);

    // Resolves a quantity entered against enteredUnitId (or directly against the article's own
    // PurchaseUnitId when unitToken is null) to a PurchaseUnitId-normalized value, per
    // ArticleUnitConversion. Same helper shape as RequisitionService's own copy — see
    // .claude/RequisitionsModule.md's "unit-aware quantities" section for the full design.
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

    public async Task<StockLevelDto?> CreateAdjustmentAsync(Guid warehouseToken, Guid articleToken, decimal deltaQuantity, Guid? unitToken, string reason, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);

        if (warehouse is null)
            throw new ApiException(ErrorCodes.InventoryWarehouseNotFound, "Warehouse not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, warehouse.OrganizationId, warehouse.WarehouseId))
            throw new ApiException(ErrorCodes.InventoryForbidden, "Cannot adjust inventory for a warehouse outside your scope.", 403);

        if (!warehouse.IsInventoriable)
            throw new ApiException(ErrorCodes.InventoryWarehouseNotInventoriable, "This warehouse does not track inventory.", 400);

        if (!warehouse.CanAdjustInventory)
            throw new ApiException(ErrorCodes.InventoryWarehouseCannotAdjust, "This warehouse is not configured to adjust inventory.", 400);

        await EnsureNoActiveCountInProgressAsync(connection, warehouse.WarehouseId, "This warehouse");

        if (deltaQuantity == 0)
            throw new ApiException(ErrorCodes.InventoryInvalidAdjustment, "The adjustment quantity cannot be zero.", 400);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ApiException(ErrorCodes.InventoryInvalidAdjustment, "A reason is required for an inventory adjustment.", 400);

        var article = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = articleToken }, commandType: CommandType.StoredProcedure);

        if (article is null)
            throw new ApiException(ErrorCodes.InventoryArticleNotFound, "Article not found.", 404);

        var resolvedQuantity = await ResolveArticleQuantityAsync(
            connection, null, mapper, article.ArticleId, article.PurchaseUnitId, article.Name, unitToken, deltaQuantity);
        var normalizedDelta = resolvedQuantity.Normalized;

        var existing = await connection.QueryFirstOrDefaultAsync<StockLevel>(
            "sp_StockLevel_GetByWarehouseAndArticle", new { warehouse.WarehouseId, article.ArticleId }, commandType: CommandType.StoredProcedure);

        var currentQuantity = existing?.QuantityOnHand ?? 0m;
        var newQuantity = currentQuantity + normalizedDelta;
        if (newQuantity < 0)
            throw new ApiException(ErrorCodes.InventoryNegativeStockNotAllowed, $"This adjustment would leave on-hand quantity at {newQuantity} for '{article.Name}' — cannot go negative.", 400);

        var actor = context.ActorUserToken.ToString();

        // Balance update + audit-trail insert are atomic — a partial write here would leave a
        // StockLevel that doesn't match its own movement ledger.
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var updated = await connection.QueryFirstOrDefaultAsync<StockLevel>(
                "sp_StockLevel_ApplyDelta",
                new { warehouse.WarehouseId, article.ArticleId, Delta = normalizedDelta, ActorBy = actor },
                transaction, commandType: CommandType.StoredProcedure);

            if (updated is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var movementParams = new DynamicParameters();
            movementParams.Add("@InventoryMovementToken", Guid.NewGuid());
            movementParams.Add("@WarehouseId", warehouse.WarehouseId);
            movementParams.Add("@ArticleId", article.ArticleId);
            movementParams.Add("@Type", InventoryMovementTypeCodes.Adjustment);
            movementParams.Add("@Quantity", normalizedDelta);
            movementParams.Add("@GoodsReceiptLineId", (int?)null);
            movementParams.Add("@InventoryTransferLineId", (int?)null);
            movementParams.Add("@EnteredUnitId", resolvedQuantity.UnitId);
            movementParams.Add("@EnteredQuantity", resolvedQuantity.RawQuantity);
            movementParams.Add("@Reason", reason);
            movementParams.Add("@CreatedBy", actor);

            await connection.ExecuteAsync("sp_InventoryMovement_Create", movementParams, transaction, commandType: CommandType.StoredProcedure);

            await transaction.CommitAsync(cancellationToken);

            return mapper.Map<StockLevelDto>(updated);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private sealed class ValidatedTransferLine
    {
        public required Article Article { get; init; }
        public required ResolvedArticleQuantity Quantity { get; init; }
        public string? Notes { get; init; }
    }

    public async Task<InventoryTransferDto?> CreateTransferAsync(Guid fromWarehouseToken, Guid toWarehouseToken, string? notes, List<CreateInventoryTransferLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var fromWarehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = fromWarehouseToken }, commandType: CommandType.StoredProcedure);
        if (fromWarehouse is null)
            throw new ApiException(ErrorCodes.InventoryWarehouseNotFound, "Source warehouse not found.", 404);

        var toWarehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = toWarehouseToken }, commandType: CommandType.StoredProcedure);
        if (toWarehouse is null)
            throw new ApiException(ErrorCodes.InventoryWarehouseNotFound, "Destination warehouse not found.", 404);

        if (fromWarehouse.WarehouseId == toWarehouse.WarehouseId)
            throw new ApiException(ErrorCodes.InventoryTransferSameWarehouse, "Source and destination warehouse must be different.", 400);

        if (!await CanManageOrganizationAsync(connection, context, fromWarehouse.OrganizationId))
            throw new ApiException(ErrorCodes.InventoryForbidden, "Cannot transfer inventory from a warehouse outside your scope.", 403);

        // A warehouse-scoped caller must be on at least one side of the transfer (sending stock
        // out or receiving it in) — separate from the org check above since either warehouse is a
        // legitimate target, unlike every other write here which has exactly one warehouse to match.
        if (!WarehouseScopeGuard.AllowsEither(context, fromWarehouse.WarehouseId, toWarehouse.WarehouseId))
            throw new ApiException(ErrorCodes.InventoryForbidden, "Cannot transfer inventory between warehouses outside your scope.", 403);

        // Cross-organization transfer is a bigger feature (SUPER_ASSOCIATE-level authorization)
        // with no driver today — deliberately out of scope, see .claude/InventoryModule.md.
        if (fromWarehouse.OrganizationId != toWarehouse.OrganizationId)
            throw new ApiException(ErrorCodes.InventoryTransferCrossOrganization, "Both warehouses must belong to the same organization.", 400);

        if (!fromWarehouse.IsInventoriable || !toWarehouse.IsInventoriable)
            throw new ApiException(ErrorCodes.InventoryWarehouseNotInventoriable, "Both warehouses must track inventory.", 400);

        if (!fromWarehouse.CanTransferOut)
            throw new ApiException(ErrorCodes.InventoryWarehouseCannotTransferOut, "The source warehouse is not configured to transfer inventory out.", 400);

        if (!toWarehouse.CanReceiveTransfers)
            throw new ApiException(ErrorCodes.InventoryWarehouseCannotReceiveTransfers, "The destination warehouse is not configured to receive transfers.", 400);

        await EnsureNoActiveCountInProgressAsync(connection, fromWarehouse.WarehouseId, "The source warehouse");
        await EnsureNoActiveCountInProgressAsync(connection, toWarehouse.WarehouseId, "The destination warehouse");

        if (lines.Count == 0)
            throw new ApiException(ErrorCodes.InventoryTransferEmpty, "At least one line must be transferred.", 400);

        // Batched — one round trip for every StockLevel in the source warehouse (same SP
        // InventoryPeriodService.CloseAsync already uses) instead of one
        // sp_StockLevel_GetByWarehouseAndArticle call per line. The Article-by-token lookup below
        // stays per-line: sp_Article_GetByToken's full visibility/favorite/classification CTEs
        // would need a real batch-by-tokens SP variant to replicate safely, and this call site
        // already runs with @ContextRoleLevel defaulted to 100 (bypass) — not a trivial reuse of
        // an existing simple batch SP the way the StockLevel half is.
        var stockByArticle = (await connection.QueryAsync<StockLevel>(
            "sp_StockLevel_GetAllByWarehouseId", new { fromWarehouse.WarehouseId }, commandType: CommandType.StoredProcedure))
            .ToDictionary(s => s.ArticleId, s => s.QuantityOnHand);

        var validatedLines = new List<ValidatedTransferLine>();
        var requestedArticleIds = new HashSet<int>();

        foreach (var input in lines)
        {
            if (input.Quantity <= 0)
                throw new ApiException(ErrorCodes.InventoryInvalidAdjustment, "Transfer quantity must be greater than zero.", 400);

            var article = await connection.QueryFirstOrDefaultAsync<Article>(
                "sp_Article_GetByToken", new { ArticleToken = input.ArticleToken }, commandType: CommandType.StoredProcedure);

            if (article is null)
                throw new ApiException(ErrorCodes.InventoryArticleNotFound, $"Article '{input.ArticleToken}' not found.", 404);

            if (!requestedArticleIds.Add(article.ArticleId))
                throw new ApiException(ErrorCodes.InventoryInvalidAdjustment, $"Article '{article.Name}' was submitted more than once.", 400);

            var resolvedQuantity = await ResolveArticleQuantityAsync(
                connection, null, mapper, article.ArticleId, article.PurchaseUnitId, article.Name, input.UnitToken, input.Quantity);

            var currentQuantity = stockByArticle.GetValueOrDefault(article.ArticleId, 0m);
            if (currentQuantity - resolvedQuantity.Normalized < 0)
                throw new ApiException(ErrorCodes.InventoryNegativeStockNotAllowed, $"Cannot transfer {resolvedQuantity.Normalized} of '{article.Name}' — only {currentQuantity} available at the source warehouse.", 400);

            validatedLines.Add(new ValidatedTransferLine { Article = article, Quantity = resolvedQuantity, Notes = input.Notes });
        }

        var actor = context.ActorUserToken.ToString();

        // Header + lines + both warehouses' balance updates + both movement rows are inserted
        // atomically per line — a partial write here would leave stock created out of thin air
        // (or destroyed) on one side of the transfer without its matching counterpart.
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var headerParams = new DynamicParameters();
            headerParams.Add("@InventoryTransferToken", Guid.NewGuid());
            headerParams.Add("@FromWarehouseId", fromWarehouse.WarehouseId);
            headerParams.Add("@ToWarehouseId", toWarehouse.WarehouseId);
            headerParams.Add("@Notes", notes);
            headerParams.Add("@CreatedBy", actor);

            var header = await connection.QueryFirstOrDefaultAsync<InventoryTransfer>(
                "sp_InventoryTransfer_Create", headerParams, transaction, commandType: CommandType.StoredProcedure);

            if (header is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            foreach (var validated in validatedLines)
            {
                var lineParams = new DynamicParameters();
                lineParams.Add("@InventoryTransferLineToken", Guid.NewGuid());
                lineParams.Add("@InventoryTransferId", header.InventoryTransferId);
                lineParams.Add("@ArticleId", validated.Article.ArticleId);
                lineParams.Add("@Quantity", validated.Quantity.Normalized);
                lineParams.Add("@TransferredUnitId", validated.Quantity.UnitId);
                lineParams.Add("@TransferredQuantity", validated.Quantity.RawQuantity);
                lineParams.Add("@Notes", validated.Notes);
                lineParams.Add("@CreatedBy", actor);

                var line = await connection.QueryFirstOrDefaultAsync<InventoryTransferLine>(
                    "sp_InventoryTransferLine_Create", lineParams, transaction, commandType: CommandType.StoredProcedure);

                if (line is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                await connection.ExecuteAsync(
                    "sp_StockLevel_ApplyDelta",
                    new { fromWarehouse.WarehouseId, validated.Article.ArticleId, Delta = -validated.Quantity.Normalized, ActorBy = actor },
                    transaction, commandType: CommandType.StoredProcedure);

                var outMovementParams = new DynamicParameters();
                outMovementParams.Add("@InventoryMovementToken", Guid.NewGuid());
                outMovementParams.Add("@WarehouseId", fromWarehouse.WarehouseId);
                outMovementParams.Add("@ArticleId", validated.Article.ArticleId);
                outMovementParams.Add("@Type", InventoryMovementTypeCodes.TransferOut);
                outMovementParams.Add("@Quantity", -validated.Quantity.Normalized);
                outMovementParams.Add("@GoodsReceiptLineId", (int?)null);
                outMovementParams.Add("@InventoryTransferLineId", line.InventoryTransferLineId);
                outMovementParams.Add("@EnteredUnitId", validated.Quantity.UnitId);
                outMovementParams.Add("@EnteredQuantity", validated.Quantity.RawQuantity.HasValue ? -validated.Quantity.RawQuantity.Value : (decimal?)null);
                outMovementParams.Add("@Reason", (string?)null);
                outMovementParams.Add("@CreatedBy", actor);
                await connection.ExecuteAsync("sp_InventoryMovement_Create", outMovementParams, transaction, commandType: CommandType.StoredProcedure);

                await connection.ExecuteAsync(
                    "sp_StockLevel_ApplyDelta",
                    new { toWarehouse.WarehouseId, validated.Article.ArticleId, Delta = validated.Quantity.Normalized, ActorBy = actor },
                    transaction, commandType: CommandType.StoredProcedure);

                var inMovementParams = new DynamicParameters();
                inMovementParams.Add("@InventoryMovementToken", Guid.NewGuid());
                inMovementParams.Add("@WarehouseId", toWarehouse.WarehouseId);
                inMovementParams.Add("@ArticleId", validated.Article.ArticleId);
                inMovementParams.Add("@Type", InventoryMovementTypeCodes.TransferIn);
                inMovementParams.Add("@Quantity", validated.Quantity.Normalized);
                inMovementParams.Add("@GoodsReceiptLineId", (int?)null);
                inMovementParams.Add("@InventoryTransferLineId", line.InventoryTransferLineId);
                inMovementParams.Add("@EnteredUnitId", validated.Quantity.UnitId);
                inMovementParams.Add("@EnteredQuantity", validated.Quantity.RawQuantity);
                inMovementParams.Add("@Reason", (string?)null);
                inMovementParams.Add("@CreatedBy", actor);
                await connection.ExecuteAsync("sp_InventoryMovement_Create", inMovementParams, transaction, commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync(cancellationToken);

            var dto = mapper.Map<InventoryTransferDto>(header);
            dto.Lines = mapper.MapList<InventoryTransferLineDto>(
                await connection.QueryAsync<InventoryTransferLine>(
                    "sp_InventoryTransferLine_GetByTransferId", new { header.InventoryTransferId }, commandType: CommandType.StoredProcedure));
            dto.LineCount = dto.Lines.Count;

            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResult<StockLevelDto>> GetStockLevelsAsync(Guid? warehouseToken, Guid? articleToken, string? searchText, int? familyId, int? subFamilyId, int? categoryId, int? subCategoryId, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        // Defaults to the caller's own WarehouseId (WarehouseContact login) so an unfiltered
        // request never falls through to "every warehouse in the org" — an explicit
        // warehouseToken is still validated against it below.
        int? warehouseId = context.WarehouseId;
        int? rootOrganizationId = null;

        if (warehouseToken.HasValue)
        {
            var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
                "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken.Value }, commandType: CommandType.StoredProcedure);

            if (warehouse is null || !await CanReadOrganizationAsync(connection, context, warehouse.OrganizationId, warehouse.WarehouseId))
                return new PagedResult<StockLevelDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            warehouseId = warehouse.WarehouseId;
        }
        else if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            rootOrganizationId = null; // unrestricted
        }
        else if (context.OrganizationId.HasValue)
        {
            rootOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            return new PagedResult<StockLevelDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        int? articleId = null;
        if (articleToken.HasValue)
        {
            var article = await connection.QueryFirstOrDefaultAsync<Article>(
                "sp_Article_GetByToken", new { ArticleToken = articleToken.Value }, commandType: CommandType.StoredProcedure);

            if (article is null)
                return new PagedResult<StockLevelDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            articleId = article.ArticleId;
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@WarehouseId", warehouseId);
        p.Add("@ArticleId", articleId);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim());
        p.Add("@FamilyId", familyId);
        p.Add("@SubFamilyId", subFamilyId);
        p.Add("@CategoryId", categoryId);
        p.Add("@SubCategoryId", subCategoryId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<StockLevelPageRow>(
            "sp_StockLevel_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<StockLevelDto>
        {
            Items = mapper.MapList<StockLevelDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<PagedResult<InventoryMovementDto>> GetMovementsAsync(Guid warehouseToken, Guid? articleToken, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);

        if (warehouse is null || !await CanReadOrganizationAsync(connection, context, warehouse.OrganizationId, warehouse.WarehouseId))
            return new PagedResult<InventoryMovementDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

        int? articleId = null;
        if (articleToken.HasValue)
        {
            var article = await connection.QueryFirstOrDefaultAsync<Article>(
                "sp_Article_GetByToken", new { ArticleToken = articleToken.Value }, commandType: CommandType.StoredProcedure);

            if (article is null)
                return new PagedResult<InventoryMovementDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            articleId = article.ArticleId;
        }

        var p = new DynamicParameters();
        p.Add("@WarehouseId", warehouse.WarehouseId);
        p.Add("@ArticleId", articleId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<InventoryMovementPageRow>(
            "sp_InventoryMovement_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<InventoryMovementDto>
        {
            Items = mapper.MapList<InventoryMovementDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<PagedResult<InventoryTransferDto>> GetTransfersAsync(Guid? warehouseToken, int? organizationId, DateTime? fromDate, DateTime? toDate, Guid? articleToken, int? familyId, int? subFamilyId, int? categoryId, int? subCategoryId, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        // Defaults to the caller's own WarehouseId (WarehouseContact login) so an unfiltered
        // request never falls through to "every warehouse in the org" — an explicit
        // warehouseToken/organizationId override is still validated against it below.
        int? warehouseId = context.WarehouseId;
        int? rootOrganizationId = null;

        if (warehouseToken.HasValue)
        {
            var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
                "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken.Value }, commandType: CommandType.StoredProcedure);

            if (warehouse is null || !await CanReadOrganizationAsync(connection, context, warehouse.OrganizationId, warehouse.WarehouseId))
                return new PagedResult<InventoryTransferDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            warehouseId = warehouse.WarehouseId;
        }
        else if (organizationId.HasValue)
        {
            // Explicit organization override (see GetInventoryTransfersQueryRequest.OrganizationToken)
            // — the Handler already validated this via GetOrganizationByTokenAsync, but re-checking
            // here is the same defense-in-depth this service applies to every other scope resolution.
            if (!await CanReadOrganizationAsync(connection, context, organizationId.Value))
                return new PagedResult<InventoryTransferDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            rootOrganizationId = organizationId.Value;
        }
        else if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            rootOrganizationId = null; // unrestricted
        }
        else if (context.OrganizationId.HasValue)
        {
            rootOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            return new PagedResult<InventoryTransferDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        int? articleId = null;
        if (articleToken.HasValue)
        {
            var article = await connection.QueryFirstOrDefaultAsync<Article>(
                "sp_Article_GetByToken", new { ArticleToken = articleToken.Value }, commandType: CommandType.StoredProcedure);

            if (article is null)
                return new PagedResult<InventoryTransferDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            articleId = article.ArticleId;
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@WarehouseId", warehouseId);
        p.Add("@FromDate", fromDate?.Date);
        p.Add("@ToDate", toDate?.Date);
        p.Add("@ArticleId", articleId);
        p.Add("@FamilyId", familyId);
        p.Add("@SubFamilyId", subFamilyId);
        p.Add("@CategoryId", categoryId);
        p.Add("@SubCategoryId", subCategoryId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<InventoryTransferPageRow>(
            "sp_InventoryTransfer_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<InventoryTransferDto>
        {
            Items = mapper.MapList<InventoryTransferDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<InventoryTransferDto?> GetTransferByTokenAsync(Guid transferToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var header = await connection.QueryFirstOrDefaultAsync<InventoryTransfer>(
            "sp_InventoryTransfer_GetByToken", new { InventoryTransferToken = transferToken }, commandType: CommandType.StoredProcedure);

        if (header is null)
            return null;

        if (!await CanReadOrganizationAsync(connection, context, header.FromOrganizationId!.Value))
            return null;

        if (!WarehouseScopeGuard.AllowsEither(context, header.FromWarehouseId, header.ToWarehouseId))
            return null;

        var dto = mapper.Map<InventoryTransferDto>(header);
        dto.Lines = mapper.MapList<InventoryTransferLineDto>(
            await connection.QueryAsync<InventoryTransferLine>(
                "sp_InventoryTransferLine_GetByTransferId", new { header.InventoryTransferId }, commandType: CommandType.StoredProcedure));
        dto.LineCount = dto.Lines.Count;

        return dto;
    }
}
