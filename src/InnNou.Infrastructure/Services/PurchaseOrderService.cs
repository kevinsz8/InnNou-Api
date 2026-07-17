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

public class PurchaseOrderService(IDbConnectionFactory connectionFactory, IMapper mapper) : IPurchaseOrderService
{
    private sealed class PurchaseOrderPageRow : PurchaseOrder { public int TotalCount { get; set; } }

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    // Read visibility, no RoleLevel floor — matches WarehouseService.CanManageReadAsync. The
    // owning Supplier branch is checked separately by callers before falling back to this.
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

    // Write visibility (Cancel) — same shape plus the StaffRoleLevel floor, mirrors
    // OrderService.CanManageOrganizationAsync.
    private static async Task<bool> CanManageOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (context.RoleLevel < StaffRoleLevel || !context.OrganizationId.HasValue)
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    private static async Task<List<PurchaseOrderLine>> GetLinesForPurchaseOrderAsync(IDbConnection connection, int purchaseOrderId)
    {
        var lines = await connection.QueryAsync<PurchaseOrderLine>(
            "sp_PurchaseOrderLine_GetByPurchaseOrderId", new { PurchaseOrderId = purchaseOrderId }, commandType: CommandType.StoredProcedure);
        return lines.ToList();
    }

    public async Task<PagedResult<PurchaseOrderDto>> GetPagedAsync(Guid? organizationToken, Guid? orderToken, string? status, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        // Purely an additional narrowing filter layered on top of the scope resolved below —
        // never widens what the caller could already see, same rule the RoleIds/OrganizationIds
        // multi-value filter on GetUsers established.
        int? orderId = null;
        if (orderToken.HasValue)
        {
            var order = await connection.QueryFirstOrDefaultAsync<Order>(
                "sp_Order_GetByToken", new { OrderToken = orderToken.Value }, commandType: CommandType.StoredProcedure);

            if (order is null)
                return new PagedResult<PurchaseOrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            orderId = order.OrderId;
        }

        int? rootOrganizationId = null;
        int? supplierId = null;

        if (context.SupplierId.HasValue)
        {
            supplierId = context.SupplierId.Value;
        }
        else if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            rootOrganizationId = null; // unrestricted
        }
        else if (organizationToken.HasValue)
        {
            var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                "sp_Organization_GetByToken",
                new { OrganizationToken = organizationToken.Value, RootOrganizationId = (int?)null },
                commandType: CommandType.StoredProcedure);

            if (organization is null || !await CanReadOrganizationAsync(connection, context, organization.OrganizationId))
                return new PagedResult<PurchaseOrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            rootOrganizationId = organization.OrganizationId;
        }
        else if (context.OrganizationId.HasValue)
        {
            rootOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            return new PagedResult<PurchaseOrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@SupplierId", supplierId);
        p.Add("@OrderId", orderId);
        p.Add("@Status", status);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<PurchaseOrderPageRow>(
            "sp_PurchaseOrder_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<PurchaseOrderDto>
        {
            Items = mapper.MapList<PurchaseOrderDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<PurchaseOrderDto?> GetByTokenAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (purchaseOrder is null)
            return null;

        var canView = context.SupplierId.HasValue
            ? context.SupplierId.Value == purchaseOrder.SupplierId
            : await CanReadOrganizationAsync(connection, context, purchaseOrder.OrganizationId);

        if (!canView)
            return null;

        var dto = mapper.Map<PurchaseOrderDto>(purchaseOrder);
        dto.Lines = mapper.MapList<PurchaseOrderLineDto>(
            await GetLinesForPurchaseOrderAsync(connection, purchaseOrder.PurchaseOrderId));
        return dto;
    }

    public async Task<PurchaseOrderDto?> CancelAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        var canManage = context.SupplierId.HasValue
            ? context.SupplierId.Value == existing.SupplierId
            : await CanManageOrganizationAsync(connection, context, existing.OrganizationId);

        if (!canManage)
            throw new ApiException(ErrorCodes.PurchaseOrderForbidden, "Cannot cancel a purchase order outside your scope.", 403);

        if (existing.Status != "SENT")
            throw new ApiException(ErrorCodes.PurchaseOrderNotSent, "Only a sent purchase order can be cancelled.", 409);

        var updated = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_Cancel",
            new { PurchaseOrderToken = purchaseOrderToken, CancelledBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        if (updated is null)
            return null;

        var dto = mapper.Map<PurchaseOrderDto>(updated);
        dto.Lines = mapper.MapList<PurchaseOrderLineDto>(
            await GetLinesForPurchaseOrderAsync(connection, updated.PurchaseOrderId));
        return dto;
    }
}
