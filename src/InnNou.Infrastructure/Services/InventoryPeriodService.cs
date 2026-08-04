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
    private static async Task<bool> CanReadOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (!context.OrganizationId.HasValue)
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
    private static async Task<bool> CanManageOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.OrganizationTypeCode != OrganizationTypeCodes.Associate)
            return false;

        if (context.RoleLevel < StaffRoleLevel || !context.OrganizationId.HasValue)
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    private async Task<InventoryPeriodDto> HydrateAsync(IDbConnection connection, InventoryPeriod header, IDbTransaction? transaction = null)
    {
        var dto = mapper.Map<InventoryPeriodDto>(header);
        dto.Lines = mapper.MapList<InventoryPeriodCountDto>(
            await connection.QueryAsync<InventoryPeriodCount>(
                "sp_InventoryPeriodCount_GetByPeriodId", new { header.InventoryPeriodId }, transaction, commandType: CommandType.StoredProcedure));
        dto.LineCount = dto.Lines.Count;
        return dto;
    }

    public async Task<InventoryPeriodDto?> OpenAsync(Guid warehouseToken, string? notes, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);

        if (warehouse is null)
            throw new ApiException(ErrorCodes.InventoryWarehouseNotFound, "Warehouse not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, warehouse.OrganizationId))
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

    public async Task<InventoryPeriodDto?> SubmitCountAsync(Guid periodToken, Guid articleToken, decimal countedQuantity, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var period = await connection.QueryFirstOrDefaultAsync<InventoryPeriod>(
            "sp_InventoryPeriod_GetByToken", new { InventoryPeriodToken = periodToken }, commandType: CommandType.StoredProcedure);

        if (period is null)
            throw new ApiException(ErrorCodes.InventoryPeriodNotFound, "Inventory period not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, period.OrganizationId))
            throw new ApiException(ErrorCodes.InventoryForbidden, "Cannot submit a count for an inventory period outside your scope.", 403);

        if (period.Status == InventoryPeriodStatus.Closed)
            throw new ApiException(ErrorCodes.InventoryPeriodAlreadyClosed, "This inventory period is already closed.", 409);

        if (countedQuantity < 0)
            throw new ApiException(ErrorCodes.InventoryPeriodInvalidCount, "Counted quantity cannot be negative.", 400);

        var article = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = articleToken }, commandType: CommandType.StoredProcedure);

        if (article is null)
            throw new ApiException(ErrorCodes.InventoryArticleNotFound, "Article not found.", 404);

        var actor = context.ActorUserToken.ToString();

        var updatedLine = await connection.QueryFirstOrDefaultAsync<InventoryPeriodCount>(
            "sp_InventoryPeriodCount_UpdateCount",
            new { period.InventoryPeriodId, article.ArticleId, CountedQuantity = countedQuantity, ActorBy = actor },
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

        if (!await CanManageOrganizationAsync(connection, context, period.OrganizationId))
            throw new ApiException(ErrorCodes.InventoryForbidden, "Cannot close an inventory period outside your scope.", 403);

        if (period.Status == InventoryPeriodStatus.Closed)
            throw new ApiException(ErrorCodes.InventoryPeriodAlreadyClosed, "This inventory period is already closed.", 409);

        if (period.Status != InventoryPeriodStatus.Pre_Closed)
            throw new ApiException(ErrorCodes.InventoryPeriodIncomplete, "Every article must be counted before this inventory period can be closed.", 409);

        var lines = (await connection.QueryAsync<InventoryPeriodCount>(
            "sp_InventoryPeriodCount_GetByPeriodId", new { period.InventoryPeriodId }, commandType: CommandType.StoredProcedure)).ToList();

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
                var countedQuantity = line.CountedQuantity!.Value; // every line is counted by construction of PRE_CLOSED
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

        if (!await CanManageOrganizationAsync(connection, context, period.OrganizationId))
            throw new ApiException(ErrorCodes.InventoryForbidden, "Cannot reopen an inventory period outside your scope.", 403);

        if (period.Status != InventoryPeriodStatus.Closed)
            throw new ApiException(ErrorCodes.InventoryPeriodNotClosed, "Only a closed inventory period can be reopened.", 409);

        var mostRecent = await connection.QueryFirstOrDefaultAsync<InventoryPeriod>(
            "sp_InventoryPeriod_GetMostRecentByWarehouseId", new { period.WarehouseId }, commandType: CommandType.StoredProcedure);

        if (mostRecent is null || mostRecent.InventoryPeriodToken != periodToken)
            throw new ApiException(ErrorCodes.InventoryPeriodNotMostRecent, "Only the most recently closed inventory period for this warehouse can be reopened.", 409);

        var lines = (await connection.QueryAsync<InventoryPeriodCount>(
            "sp_InventoryPeriodCount_GetByPeriodId", new { period.InventoryPeriodId }, commandType: CommandType.StoredProcedure)).ToList();

        var actor = context.ActorUserToken.ToString();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var line in lines.Where(l => l.VarianceQuantity is not (null or 0)))
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

            var statusParams = new DynamicParameters();
            statusParams.Add("@InventoryPeriodToken", periodToken);
            statusParams.Add("@Status", InventoryPeriodStatusCodes.PreClosed);
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

        int? warehouseId = null;
        int? rootOrganizationId = null;

        if (warehouseToken.HasValue)
        {
            var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
                "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken.Value }, commandType: CommandType.StoredProcedure);

            if (warehouse is null || !await CanReadOrganizationAsync(connection, context, warehouse.OrganizationId))
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

        if (header is null || !await CanReadOrganizationAsync(connection, context, header.OrganizationId))
            return null;

        return await HydrateAsync(connection, header);
    }
}
