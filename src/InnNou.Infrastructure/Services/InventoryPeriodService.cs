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

public class InventoryPeriodService(IDbConnectionFactory connectionFactory, IMapper mapper) : IInventoryPeriodService
{
    private sealed class InventoryPeriodPageRow : InventoryPeriod { public int TotalCount { get; set; } }

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int AdminRoleLevel = 80;
    private const int MaxPageSize = 100;

    // Copied verbatim from InventoryService — read visibility, no OrganizationTypeCode
    // restriction.
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

    // Copied verbatim from InventoryService — write visibility: only a caller whose own
    // organization is ASSOCIATE may write; SuperAdmin (no organization of their own, unless
    // impersonating) and SUPER_ASSOCIATE are read-only — inventory counting happens at the
    // property level, same reasoning as Orders/Goods Receipts/Inventory itself.
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

    private readonly record struct ResolvedArticleQuantity(decimal Normalized, int? UnitId, decimal? RawQuantity);

    // Copied verbatim from InventoryService/RequisitionService — resolves a quantity entered
    // against enteredUnitId (or directly against the article's own PurchaseUnitId when
    // unitToken is null) to a PurchaseUnitId-normalized value, per ArticleUnitConversion. See
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

    private async Task<InventoryPeriodDto> HydrateAsync(IDbConnection connection, InventoryPeriod header, IDbTransaction? transaction = null)
    {
        var dto = mapper.Map<InventoryPeriodDto>(header);
        var lineEntities = (await connection.QueryAsync<InventoryPeriodCount>(
            "sp_InventoryPeriodCount_GetByPeriodId", new { header.InventoryPeriodId }, transaction, commandType: CommandType.StoredProcedure)).ToList();
        var lineDtos = mapper.MapList<InventoryPeriodCountDto>(lineEntities);

        // Same batched anti-N+1 "Unidad Definida" secondary reference as
        // RequisitionService.GetByTokenAsync/InventoryService — see
        // ArticleUnitConversion.GetDefinedUnitEquivalent. Only meaningful for lines that have
        // actually been counted; an uncounted line (CountedQuantity still NULL) has nothing to
        // convert.
        var articleIds = lineEntities.Where(l => l.CountedQuantity.HasValue).Select(l => l.ArticleId).Distinct().ToList();
        if (articleIds.Count > 0)
        {
            var levelRows = await connection.QueryAsync<ArticlePackagingLevel>(
                "sp_ArticlePackagingLevel_GetByArticleIds", new { ArticleIds = string.Join(',', articleIds) }, transaction, commandType: CommandType.StoredProcedure);
            var levelsByArticleId = levelRows.GroupBy(l => l.ArticleId)
                .ToDictionary(g => g.Key, g => mapper.MapList<ArticlePackagingLevelDto>(g.ToList()));

            for (var i = 0; i < lineEntities.Count; i++)
            {
                var entity = lineEntities[i];
                if (!entity.CountedQuantity.HasValue) continue;

                var levels = levelsByArticleId.GetValueOrDefault(entity.ArticleId, []);
                var effectiveUnitId = entity.CountedUnitId ?? entity.PurchaseUnitId;
                var effectiveQuantity = entity.CountedQuantityInUnit ?? entity.CountedQuantity.Value;
                var equivalent = ArticleUnitConversion.GetDefinedUnitEquivalent(entity.PurchaseUnitId, levels, effectiveUnitId, effectiveQuantity);
                if (equivalent is not null)
                {
                    lineDtos[i].DefinedUnitCode = equivalent.Value.Code;
                    lineDtos[i].DefinedUnitNameTranslations = equivalent.Value.NameTranslations;
                    lineDtos[i].DefinedUnitQuantity = equivalent.Value.Quantity;
                }
            }
        }

        dto.Lines = lineDtos;
        dto.LineCount = lineDtos.Count;
        return dto;
    }

    public async Task<InventoryPeriodDto?> OpenAsync(Guid warehouseToken, string? notes, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);

        if (warehouse is null)
            throw new ApiException(ErrorCodes.InventoryWarehouseNotFound, "Warehouse not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, warehouse.OrganizationId, warehouse.WarehouseId))
            throw new ApiException(ErrorCodes.InventoryForbidden, "Cannot open an inventory period for a warehouse outside your scope.", 403);

        if (!warehouse.IsInventoriable)
            throw new ApiException(ErrorCodes.InventoryWarehouseNotInventoriable, "This warehouse does not track inventory.", 400);

        if (!warehouse.CanCountInventory)
            throw new ApiException(ErrorCodes.InventoryPeriodWarehouseCannotCount, "This warehouse is not configured to run inventory counts.", 400);

        var active = await connection.QueryFirstOrDefaultAsync<InventoryPeriod>(
            "sp_InventoryPeriod_GetActiveByWarehouseId", new { warehouse.WarehouseId }, commandType: CommandType.StoredProcedure);

        if (active is not null)
            throw new ApiException(ErrorCodes.InventoryPeriodAlreadyOpen, "This warehouse already has an active inventory period — close it before opening a new one.", 409);

        var stockLevels = (await connection.QueryAsync<StockLevel>(
            "sp_StockLevel_GetAllByWarehouseId", new { warehouse.WarehouseId }, commandType: CommandType.StoredProcedure)).ToList();

        var actor = context.ActorUserToken.ToString();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var headerParams = new DynamicParameters();
            headerParams.Add("@InventoryPeriodToken", Guid.NewGuid());
            headerParams.Add("@WarehouseId", warehouse.WarehouseId);
            headerParams.Add("@StartDate", DateTime.UtcNow);
            headerParams.Add("@Notes", notes);
            headerParams.Add("@CreatedBy", actor);

            var header = await connection.QueryFirstOrDefaultAsync<InventoryPeriod>(
                "sp_InventoryPeriod_Create", headerParams, transaction, commandType: CommandType.StoredProcedure);

            if (header is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            foreach (var stockLevel in stockLevels)
            {
                var lineParams = new DynamicParameters();
                lineParams.Add("@InventoryPeriodCountToken", Guid.NewGuid());
                lineParams.Add("@InventoryPeriodId", header.InventoryPeriodId);
                lineParams.Add("@ArticleId", stockLevel.ArticleId);
                lineParams.Add("@OpeningQuantity", stockLevel.QuantityOnHand);
                lineParams.Add("@CreatedBy", actor);

                await connection.ExecuteAsync("sp_InventoryPeriodCount_Create", lineParams, transaction, commandType: CommandType.StoredProcedure);
            }

            // A warehouse with no StockLevel history yet (nothing ever received/adjusted there)
            // is vacuously "fully counted" the moment it's opened — skip straight to PRE_CLOSED
            // rather than leaving it permanently stuck at OPEN with nothing to submit.
            if (stockLevels.Count == 0)
            {
                var statusParams = new DynamicParameters();
                statusParams.Add("@InventoryPeriodToken", header.InventoryPeriodToken);
                statusParams.Add("@Status", InventoryPeriodStatusCodes.PreClosed);
                statusParams.Add("@ActorBy", actor);

                header = await connection.QueryFirstOrDefaultAsync<InventoryPeriod>(
                    "sp_InventoryPeriod_SetStatus", statusParams, transaction, commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync(cancellationToken);

            return await HydrateAsync(connection, header!);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<InventoryPeriodDto?> SubmitCountAsync(Guid periodToken, Guid articleToken, decimal countedQuantity, Guid? unitToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var period = await connection.QueryFirstOrDefaultAsync<InventoryPeriod>(
            "sp_InventoryPeriod_GetByToken", new { InventoryPeriodToken = periodToken }, commandType: CommandType.StoredProcedure);

        if (period is null)
            throw new ApiException(ErrorCodes.InventoryPeriodNotFound, "Inventory period not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, period.OrganizationId, period.WarehouseId))
            throw new ApiException(ErrorCodes.InventoryForbidden, "Cannot submit a count for an inventory period outside your scope.", 403);

        if (period.Status == InventoryPeriodStatus.Closed)
            throw new ApiException(ErrorCodes.InventoryPeriodAlreadyClosed, "This inventory period is already closed.", 409);

        if (countedQuantity < 0)
            throw new ApiException(ErrorCodes.InventoryPeriodInvalidCount, "Counted quantity cannot be negative.", 400);

        var article = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = articleToken }, commandType: CommandType.StoredProcedure);

        if (article is null)
            throw new ApiException(ErrorCodes.InventoryArticleNotFound, "Article not found.", 404);

        var resolvedQuantity = await ResolveArticleQuantityAsync(
            connection, null, mapper, article.ArticleId, article.PurchaseUnitId, article.Name, unitToken, countedQuantity);

        var actor = context.ActorUserToken.ToString();

        var updatedLine = await connection.QueryFirstOrDefaultAsync<InventoryPeriodCount>(
            "sp_InventoryPeriodCount_UpdateCount",
            new
            {
                period.InventoryPeriodId, article.ArticleId,
                CountedQuantity = resolvedQuantity.Normalized,
                CountedUnitId = resolvedQuantity.UnitId,
                CountedQuantityInUnit = resolvedQuantity.RawQuantity,
                ActorBy = actor
            },
            commandType: CommandType.StoredProcedure);

        if (updatedLine is null)
            throw new ApiException(ErrorCodes.InventoryPeriodArticleNotInPeriod, $"Article '{article.Name}' is not part of this inventory period.", 404);

        var allLines = (await connection.QueryAsync<InventoryPeriodCount>(
            "sp_InventoryPeriodCount_GetByPeriodId", new { period.InventoryPeriodId }, commandType: CommandType.StoredProcedure)).ToList();

        var allCounted = allLines.Count > 0 && allLines.All(l => l.CountedQuantity.HasValue);
        var targetStatusCode = allCounted ? InventoryPeriodStatusCodes.PreClosed : InventoryPeriodStatusCodes.InProgress;

        var header = period;
        if (InventoryPeriodStatusCodes.ToCode(period.Status) != targetStatusCode)
        {
            header = await connection.QueryFirstOrDefaultAsync<InventoryPeriod>(
                "sp_InventoryPeriod_SetStatus",
                new { InventoryPeriodToken = periodToken, Status = targetStatusCode, ActorBy = actor },
                commandType: CommandType.StoredProcedure) ?? period;
        }

        var dto = mapper.Map<InventoryPeriodDto>(header);
        dto.Lines = mapper.MapList<InventoryPeriodCountDto>(allLines);
        dto.LineCount = dto.Lines.Count;

        return dto;
    }

    public async Task<InventoryPeriodDto?> CloseAsync(Guid periodToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var period = await connection.QueryFirstOrDefaultAsync<InventoryPeriod>(
            "sp_InventoryPeriod_GetByToken", new { InventoryPeriodToken = periodToken }, commandType: CommandType.StoredProcedure);

        if (period is null)
            throw new ApiException(ErrorCodes.InventoryPeriodNotFound, "Inventory period not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, period.OrganizationId, period.WarehouseId))
            throw new ApiException(ErrorCodes.InventoryForbidden, "Cannot close an inventory period outside your scope.", 403);

        if (period.Status == InventoryPeriodStatus.Closed)
            throw new ApiException(ErrorCodes.InventoryPeriodAlreadyClosed, "This inventory period is already closed.", 409);

        if (period.Status == InventoryPeriodStatus.Open)
            throw new ApiException(ErrorCodes.InventoryPeriodIncomplete, "At least one article must be counted before this inventory period can be closed.", 409);

        // Partial close (SAP-style): a period can be closed from IN_PROGRESS, not only
        // PRE_CLOSED. Only counted lines get a variance computed and an ADJUSTMENT posted —
        // uncounted lines are left untouched (SystemQuantityAtClose/VarianceQuantity stay NULL,
        // a permanent, queryable "never verified this cycle" fact) and simply reappear, freshly
        // seeded, the next time this warehouse's period is opened. No separate "resume" mechanic
        // is needed for them — the next Open already re-seeds every current StockLevel row.
        var lines = (await connection.QueryAsync<InventoryPeriodCount>(
            "sp_InventoryPeriodCount_GetByPeriodId", new { period.InventoryPeriodId }, commandType: CommandType.StoredProcedure))
            .Where(l => l.CountedQuantity.HasValue)
            .ToList();

        // Batched — one round trip for every StockLevel in the warehouse (same SP OpenAsync
        // already uses) instead of one sp_StockLevel_GetByWarehouseAndArticle call per counted
        // line inside the transaction below. Still "live at the moment of close" (fetched right
        // before the transaction opens, not a stale snapshot from when counting started) — this
        // only removes the per-line round trip, it doesn't change which balance is read. Cuts the
        // transaction's per-line work from up to 3 round trips down to up to 2, meaningfully
        // reducing how long locks on StockLevels/InventoryMovements are held for a large warehouse.
        var currentStockByArticle = (await connection.QueryAsync<StockLevel>(
            "sp_StockLevel_GetAllByWarehouseId", new { period.WarehouseId }, commandType: CommandType.StoredProcedure))
            .ToDictionary(s => s.ArticleId, s => s.QuantityOnHand);

        var actor = context.ActorUserToken.ToString();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var line in lines)
            {
                // Live balance at the moment of close — never a stale snapshot taken when the
                // period opened or when counting started, so a receipt/adjustment/transfer that
                // legitimately happened mid-count is reflected honestly in the variance.
                var systemQuantity = currentStockByArticle.GetValueOrDefault(line.ArticleId, 0m);
                var countedQuantity = line.CountedQuantity!.Value; // guaranteed by the .Where(HasValue) filter above
                var variance = countedQuantity - systemQuantity;

                var varianceParams = new DynamicParameters();
                varianceParams.Add("@InventoryPeriodCountId", line.InventoryPeriodCountId);
                varianceParams.Add("@SystemQuantityAtClose", systemQuantity);
                varianceParams.Add("@VarianceQuantity", variance);
                varianceParams.Add("@ActorBy", actor);
                await connection.ExecuteAsync("sp_InventoryPeriodCount_UpdateVariance", varianceParams, transaction, commandType: CommandType.StoredProcedure);

                if (variance != 0)
                {
                    // new balance after applying = systemQuantity + variance = countedQuantity,
                    // and countedQuantity >= 0 was already enforced at submit time — the negative-
                    // stock guard in sp_StockLevel_ApplyDelta can never actually fire here.
                    await connection.ExecuteAsync(
                        "sp_StockLevel_ApplyDelta",
                        new { period.WarehouseId, line.ArticleId, Delta = variance, ActorBy = actor },
                        transaction, commandType: CommandType.StoredProcedure);

                    var movementParams = new DynamicParameters();
                    movementParams.Add("@InventoryMovementToken", Guid.NewGuid());
                    movementParams.Add("@WarehouseId", period.WarehouseId);
                    movementParams.Add("@ArticleId", line.ArticleId);
                    movementParams.Add("@Type", InventoryMovementTypeCodes.Adjustment);
                    movementParams.Add("@Quantity", variance);
                    movementParams.Add("@GoodsReceiptLineId", (int?)null);
                    movementParams.Add("@InventoryTransferLineId", (int?)null);
                    movementParams.Add("@InventoryPeriodCountId", line.InventoryPeriodCountId);
                    movementParams.Add("@Reason", "Inventory period close");
                    movementParams.Add("@CreatedBy", actor);
                    await connection.ExecuteAsync("sp_InventoryMovement_Create", movementParams, transaction, commandType: CommandType.StoredProcedure);
                }
            }

            var statusParams = new DynamicParameters();
            statusParams.Add("@InventoryPeriodToken", periodToken);
            statusParams.Add("@Status", InventoryPeriodStatusCodes.Closed);
            statusParams.Add("@ActorBy", actor);
            statusParams.Add("@ClosedUtc", DateTime.UtcNow);
            statusParams.Add("@ClosedBy", actor);

            var updatedHeader = await connection.QueryFirstOrDefaultAsync<InventoryPeriod>(
                "sp_InventoryPeriod_SetStatus", statusParams, transaction, commandType: CommandType.StoredProcedure);

            if (updatedHeader is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await transaction.CommitAsync(cancellationToken);

            return await HydrateAsync(connection, updatedHeader);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<InventoryPeriodDto?> ReopenAsync(Guid periodToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var period = await connection.QueryFirstOrDefaultAsync<InventoryPeriod>(
            "sp_InventoryPeriod_GetByToken", new { InventoryPeriodToken = periodToken }, commandType: CommandType.StoredProcedure);

        if (period is null)
            throw new ApiException(ErrorCodes.InventoryPeriodNotFound, "Inventory period not found.", 404);

        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.InventoryPeriodReopenForbidden, "Reopening a closed inventory period requires an administrator.", 403);

        if (!await CanManageOrganizationAsync(connection, context, period.OrganizationId, period.WarehouseId))
            throw new ApiException(ErrorCodes.InventoryForbidden, "Cannot reopen an inventory period outside your scope.", 403);

        if (period.Status != InventoryPeriodStatus.Closed)
            throw new ApiException(ErrorCodes.InventoryPeriodNotClosed, "Only a closed inventory period can be reopened.", 409);

        var mostRecent = await connection.QueryFirstOrDefaultAsync<InventoryPeriod>(
            "sp_InventoryPeriod_GetMostRecentByWarehouseId", new { period.WarehouseId }, commandType: CommandType.StoredProcedure);

        if (mostRecent is null || mostRecent.InventoryPeriodToken != periodToken)
            throw new ApiException(ErrorCodes.InventoryPeriodNotMostRecent, "Only the most recently closed inventory period for this warehouse can be reopened.", 409);

        var lines = (await connection.QueryAsync<InventoryPeriodCount>(
            "sp_InventoryPeriodCount_GetByPeriodId", new { period.InventoryPeriodId }, commandType: CommandType.StoredProcedure)).ToList();

        var reversedLines = lines.Where(l => l.VarianceQuantity is not (null or 0)).ToList();

        // Batched — same anti-N+1 shape as CloseAsync's own read of sp_StockLevel_GetAllByWarehouseId.
        // Unlike CloseAsync (provably safe by construction — see the comment there), a reversal here
        // is NOT provably safe: the balance can have legitimately moved since this period closed
        // (Adjustments/Transfers/Requisition-Issues/Receipts are only blocked while a period is
        // IN_PROGRESS/PRE_CLOSED, not once it's CLOSED), so the compensating reversal can genuinely
        // drive a line negative. Validate every line in a full pre-pass BEFORE the transaction opens
        // and starts writing, so a single failing line cleanly rejects the whole Reopen with no
        // partial state — mirrors sp_StockLevel_ApplyDelta's own guard, just surfaced as a clean
        // business error instead of an unhandled SqlException.
        if (reversedLines.Count > 0)
        {
            var currentStockForValidation = (await connection.QueryAsync<StockLevel>(
                "sp_StockLevel_GetAllByWarehouseId", new { period.WarehouseId }, commandType: CommandType.StoredProcedure))
                .ToDictionary(s => s.ArticleId, s => s.QuantityOnHand);

            foreach (var line in reversedLines)
            {
                var reversal = -line.VarianceQuantity!.Value;
                var currentQuantity = currentStockForValidation.GetValueOrDefault(line.ArticleId, 0m);
                var newQuantity = currentQuantity + reversal;

                if (newQuantity < 0)
                    throw new ApiException(
                        ErrorCodes.InventoryPeriodReopenWouldGoNegative,
                        $"Reopening this inventory period would leave on-hand quantity at {newQuantity} for '{line.ArticleName}' — stock has moved since this period was closed and cannot go negative.",
                        400);
            }
        }

        var actor = context.ActorUserToken.ToString();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var line in reversedLines)
            {
                var reversal = -line.VarianceQuantity!.Value;

                await connection.ExecuteAsync(
                    "sp_StockLevel_ApplyDelta",
                    new { period.WarehouseId, line.ArticleId, Delta = reversal, ActorBy = actor },
                    transaction, commandType: CommandType.StoredProcedure);

                var movementParams = new DynamicParameters();
                movementParams.Add("@InventoryMovementToken", Guid.NewGuid());
                movementParams.Add("@WarehouseId", period.WarehouseId);
                movementParams.Add("@ArticleId", line.ArticleId);
                movementParams.Add("@Type", InventoryMovementTypeCodes.Adjustment);
                movementParams.Add("@Quantity", reversal);
                movementParams.Add("@GoodsReceiptLineId", (int?)null);
                movementParams.Add("@InventoryTransferLineId", (int?)null);
                movementParams.Add("@InventoryPeriodCountId", line.InventoryPeriodCountId);
                movementParams.Add("@Reason", "Reversed: period reopened");
                movementParams.Add("@CreatedBy", actor);
                await connection.ExecuteAsync("sp_InventoryMovement_Create", movementParams, transaction, commandType: CommandType.StoredProcedure);

                var varianceParams = new DynamicParameters();
                varianceParams.Add("@InventoryPeriodCountId", line.InventoryPeriodCountId);
                varianceParams.Add("@SystemQuantityAtClose", (decimal?)null);
                varianceParams.Add("@VarianceQuantity", (decimal?)null);
                varianceParams.Add("@ActorBy", actor);
                await connection.ExecuteAsync("sp_InventoryPeriodCount_UpdateVariance", varianceParams, transaction, commandType: CommandType.StoredProcedure);
            }

            // Recompute rather than assume PRE_CLOSED — the period may have been closed early
            // (partial close, some lines never counted), and reopening it must land back on
            // whatever status honestly reflects that, same completeness check SubmitCountAsync uses.
            var allCounted = lines.Count > 0 && lines.All(l => l.CountedQuantity.HasValue);
            var reopenTargetStatus = allCounted ? InventoryPeriodStatusCodes.PreClosed : InventoryPeriodStatusCodes.InProgress;

            var statusParams = new DynamicParameters();
            statusParams.Add("@InventoryPeriodToken", periodToken);
            statusParams.Add("@Status", reopenTargetStatus);
            statusParams.Add("@ActorBy", actor);
            statusParams.Add("@ReopenedUtc", DateTime.UtcNow);
            statusParams.Add("@ReopenedBy", actor);
            statusParams.Add("@ClearClosedFields", true);

            var updatedHeader = await connection.QueryFirstOrDefaultAsync<InventoryPeriod>(
                "sp_InventoryPeriod_SetStatus", statusParams, transaction, commandType: CommandType.StoredProcedure);

            if (updatedHeader is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await transaction.CommitAsync(cancellationToken);

            return await HydrateAsync(connection, updatedHeader);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResult<InventoryPeriodDto>> GetPagedAsync(Guid? warehouseToken, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
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
                return new PagedResult<InventoryPeriodDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

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
            return new PagedResult<InventoryPeriodDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@WarehouseId", warehouseId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<InventoryPeriodPageRow>(
            "sp_InventoryPeriod_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<InventoryPeriodDto>
        {
            Items = mapper.MapList<InventoryPeriodDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<InventoryPeriodDto?> GetByTokenAsync(Guid periodToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var header = await connection.QueryFirstOrDefaultAsync<InventoryPeriod>(
            "sp_InventoryPeriod_GetByToken", new { InventoryPeriodToken = periodToken }, commandType: CommandType.StoredProcedure);

        if (header is null || !await CanReadOrganizationAsync(connection, context, header.OrganizationId, header.WarehouseId))
            return null;

        return await HydrateAsync(connection, header);
    }
}
