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

namespace InnNou.Infrastructure.Services;

public class SupplierReturnService(IDbConnectionFactory connectionFactory, IMapper mapper, INotificationService notificationService, ILogger<SupplierReturnService> logger) : ISupplierReturnService
{
    private sealed class SupplierReturnPageRow : SupplierReturn { public int TotalCount { get; set; } }

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    // Read visibility — no RoleLevel floor, matches PurchaseOrderService.CanReadOrganizationAsync.
    // Supplier read access was deliberately left out of scope for this feature (confirmed with
    // the user — 100% buyer-side, same rule GoodsReceipts/Rectifications already enforce).
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

    // Write visibility — only a caller whose own organization is ASSOCIATE may create/close a
    // return; SuperAdmin (no organization of their own, unless impersonating) and SUPER_ASSOCIATE
    // are read-only, same shape as PurchaseOrderService/OrderService's own CanManageOrganizationAsync.
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

    public async Task<List<EligibleReturnLineDto>?> GetEligibleLinesAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (purchaseOrder is null || !await CanManageOrganizationAsync(connection, context, purchaseOrder.OrganizationId))
            return null;

        var rows = await connection.QueryAsync<EligibleReturnLine>(
            "sp_GoodsReceiptLine_GetEligibleForReturn", new { purchaseOrder.PurchaseOrderId }, commandType: CommandType.StoredProcedure);

        return mapper.MapList<EligibleReturnLineDto>(rows);
    }

    public async Task<SupplierReturnDto?> CreateAsync(Guid purchaseOrderToken, string? notes, List<Guid> goodsReceiptLineTokens, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (purchaseOrder is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, purchaseOrder.OrganizationId))
            throw new ApiException(ErrorCodes.SupplierReturnForbidden, "Cannot create a supplier return for a purchase order outside your scope.", 403);

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { purchaseOrder.WarehouseToken }, commandType: CommandType.StoredProcedure);

        if (warehouse is null || !warehouse.CanReceiveReturns)
            throw new ApiException(ErrorCodes.SupplierReturnWarehouseCannotReceiveReturns, "This warehouse is not configured to process supplier returns.", 400);

        if (goodsReceiptLineTokens.Count == 0)
            throw new ApiException(ErrorCodes.SupplierReturnEmpty, "At least one rejected line must be included.", 400);

        var eligibleLines = (await connection.QueryAsync<EligibleReturnLine>(
            "sp_GoodsReceiptLine_GetEligibleForReturn", new { purchaseOrder.PurchaseOrderId }, commandType: CommandType.StoredProcedure))
            .ToDictionary(l => l.GoodsReceiptLineToken);

        var selectedLines = new List<EligibleReturnLine>();
        foreach (var token in goodsReceiptLineTokens.Distinct())
        {
            if (!eligibleLines.TryGetValue(token, out var line))
                throw new ApiException(ErrorCodes.SupplierReturnLineNotEligible, $"Goods receipt line '{token}' is not eligible for a return — already claimed, not rejected, or doesn't belong to this purchase order.", 400);
            selectedLines.Add(line);
        }

        var actor = context.ActorUserToken.ToString();

        // Header + lines inserted atomically — a partial write here would leave a return case
        // whose lines don't match what was actually claimed. Same shape as
        // PurchaseOrderService.CreateGoodsReceiptAsync/CreateRectificationAsync.
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var headerParams = new DynamicParameters();
            headerParams.Add("@SupplierReturnToken", Guid.NewGuid());
            headerParams.Add("@PurchaseOrderId", purchaseOrder.PurchaseOrderId);
            headerParams.Add("@Notes", notes);
            headerParams.Add("@CreatedBy", actor);

            var header = await connection.QueryFirstOrDefaultAsync<SupplierReturn>(
                "sp_SupplierReturn_Create", headerParams, transaction, commandType: CommandType.StoredProcedure);

            if (header is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var lines = new List<SupplierReturnLine>();
            foreach (var selected in selectedLines)
            {
                var lineParams = new DynamicParameters();
                lineParams.Add("@SupplierReturnLineToken", Guid.NewGuid());
                lineParams.Add("@SupplierReturnId", header.SupplierReturnId);
                lineParams.Add("@GoodsReceiptLineId", selected.GoodsReceiptLineId);
                lineParams.Add("@Notes", (string?)null);
                lineParams.Add("@CreatedBy", actor);

                var line = await connection.QueryFirstOrDefaultAsync<SupplierReturnLine>(
                    "sp_SupplierReturnLine_Create", lineParams, transaction, commandType: CommandType.StoredProcedure);
                if (line is not null)
                    lines.Add(line);
            }

            await transaction.CommitAsync(cancellationToken);

            var dto = mapper.Map<SupplierReturnDto>(header);
            dto.Lines = mapper.MapList<SupplierReturnLineDto>(lines);
            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<SupplierReturnDto?> CloseAsync(Guid supplierReturnToken, string resolutionType, string? notes, IRequestContext context, CancellationToken cancellationToken)
    {
        if (!SupplierReturnResolutionTypeCodes.TryFromCode(resolutionType, out _))
            throw new ApiException(ErrorCodes.SupplierReturnInvalidResolutionType, $"Unrecognized resolution type '{resolutionType}'.", 400);

        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<SupplierReturn>(
            "sp_SupplierReturn_GetByToken", new { SupplierReturnToken = supplierReturnToken }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId))
            throw new ApiException(ErrorCodes.SupplierReturnForbidden, "Cannot close a supplier return outside your scope.", 403);

        if (existing.Status != SupplierReturnStatus.Pending)
            throw new ApiException(ErrorCodes.SupplierReturnAlreadyClosed, "This supplier return is already closed.", 409);

        var actor = context.ActorUserToken.ToString();

        var header = await connection.QueryFirstOrDefaultAsync<SupplierReturn>(
            "sp_SupplierReturn_Close",
            new
            {
                existing.SupplierReturnId,
                ResolutionType = resolutionType,
                Notes = notes,
                ClosedUtc = DateTime.UtcNow,
                ClosedBy = actor
            },
            commandType: CommandType.StoredProcedure);

        if (header is null)
            return null;

        // Best-effort/non-blocking. Recipient resolved from the return's own CreatedBy (whoever
        // opened it) — never context.ActorUserToken, since a different Admin can close a return
        // someone else on the team opened.
        if (Guid.TryParse(header.CreatedBy, out var openerToken))
        {
            try
            {
                // notificationService.NotifyAsync opens its own connection — close this one first
                // (Dapper reopens it transparently on the next query below). See
                // PurchaseOrderService.NotifyOrderBuyerAsync's own comment for the full reasoning.
                await connection.CloseAsync();

                await notificationService.NotifyAsync(
                    openerToken, NotificationType.Supplier_Return_Closed,
                    new { purchaseOrderNumber = header.PurchaseOrderNumber, resolutionType },
                    $"/supplierReturns/{header.SupplierReturnToken}", context, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notification failed for SupplierReturn {SupplierReturnToken}", header.SupplierReturnToken);
            }
        }

        var dto = mapper.Map<SupplierReturnDto>(header);
        dto.Lines = mapper.MapList<SupplierReturnLineDto>(
            await connection.QueryAsync<SupplierReturnLine>(
                "sp_SupplierReturnLine_GetBySupplierReturnId", new { header.SupplierReturnId }, commandType: CommandType.StoredProcedure));
        return dto;
    }

    public async Task<SupplierReturnDto?> GetByTokenAsync(Guid supplierReturnToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var header = await connection.QueryFirstOrDefaultAsync<SupplierReturn>(
            "sp_SupplierReturn_GetByToken", new { SupplierReturnToken = supplierReturnToken }, commandType: CommandType.StoredProcedure);

        if (header is null || !await CanReadOrganizationAsync(connection, context, header.OrganizationId))
            return null;

        var dto = mapper.Map<SupplierReturnDto>(header);
        dto.Lines = mapper.MapList<SupplierReturnLineDto>(
            await connection.QueryAsync<SupplierReturnLine>(
                "sp_SupplierReturnLine_GetBySupplierReturnId", new { header.SupplierReturnId }, commandType: CommandType.StoredProcedure));
        return dto;
    }

    public async Task<PagedResult<SupplierReturnDto>> GetPagedAsync(Guid? organizationToken, Guid? supplierToken, string? status, DateTime? fromDate, DateTime? toDate, string? purchaseOrderNumber, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
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
            return new PagedResult<SupplierReturnDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        // Purely an additional narrowing filter layered on top of the scope resolved above —
        // never widens what the caller could already see, same rule the RoleIds/OrganizationIds
        // multi-value filter on GetUsers established.
        if (organizationToken.HasValue)
        {
            var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                "sp_Organization_GetByToken", new { OrganizationToken = organizationToken.Value }, commandType: CommandType.StoredProcedure);

            if (organization is null || !await CanReadOrganizationAsync(connection, context, organization.OrganizationId))
                return new PagedResult<SupplierReturnDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            rootOrganizationId = organization.OrganizationId;
        }

        int? supplierId = null;
        if (supplierToken.HasValue)
        {
            var supplier = await connection.QueryFirstOrDefaultAsync<Supplier>(
                "sp_Supplier_GetByToken", new { SupplierToken = supplierToken.Value }, commandType: CommandType.StoredProcedure);

            if (supplier is null)
                return new PagedResult<SupplierReturnDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            supplierId = supplier.SupplierId;
        }

        int? statusId = null;
        if (status is not null)
        {
            if (!SupplierReturnStatusCodes.TryFromCode(status, out var parsedStatus))
                return new PagedResult<SupplierReturnDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
            statusId = (int)parsedStatus;
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@SupplierId", supplierId);
        p.Add("@StatusId", statusId);
        p.Add("@FromDate", fromDate?.Date);
        p.Add("@ToDate", toDate?.Date);
        p.Add("@PurchaseOrderNumber", string.IsNullOrWhiteSpace(purchaseOrderNumber) ? null : purchaseOrderNumber.Trim());
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<SupplierReturnPageRow>(
            "sp_SupplierReturn_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<SupplierReturnDto>
        {
            Items = mapper.MapList<SupplierReturnDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }
}
