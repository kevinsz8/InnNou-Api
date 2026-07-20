using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class SupplierDeliveryZoneService(IDbConnectionFactory connectionFactory, IMapper mapper) : ISupplierDeliveryZoneService
{
    private const int AdminRoleLevel = 80;

    // Same two-line shape already used identically in ArticleService.CanManage /
    // ArticlePriceService.CanManage — the owning supplier (real login or an Admin
    // impersonating them, both look identical via context.SupplierId) can manage only
    // their own; an Admin/SuperAdmin with no SupplierId in context can manage any
    // supplier's coverage as long as they explicitly specify which one.
    private static bool CanManage(IRequestContext context, int supplierId) =>
        context.SupplierId.HasValue
            ? context.SupplierId.Value == supplierId
            : context.RoleLevel >= AdminRoleLevel;

    private async Task<int> ResolveSupplierIdAsync(IDbConnection connection, Guid? supplierToken, IRequestContext context)
    {
        if (supplierToken.HasValue)
        {
            var supplier = await connection.QueryFirstOrDefaultAsync<Supplier>(
                "sp_Supplier_GetByToken", new { SupplierToken = supplierToken.Value }, commandType: CommandType.StoredProcedure);
            if (supplier is null)
                throw new ApiException(ErrorCodes.SupplierNotFound, "Supplier not found.", 404);

            if (!CanManage(context, supplier.SupplierId))
                throw new ApiException(ErrorCodes.SupplierDeliveryZoneForbidden, "Cannot manage delivery zones for this supplier.", 403);

            return supplier.SupplierId;
        }

        if (!context.SupplierId.HasValue)
            throw new ApiException(ErrorCodes.SupplierDeliveryZoneNoSupplierContext, "This action requires a supplier-scoped account or an explicit supplier token.", 403);

        return context.SupplierId.Value;
    }

    private async Task<int> ResolveZoneIdAsync(IDbConnection connection, Guid zoneToken)
    {
        var zone = await connection.QueryFirstOrDefaultAsync<Zone>(
            "sp_Zone_GetByToken", new { ZoneToken = zoneToken }, commandType: CommandType.StoredProcedure);
        if (zone is null)
            throw new ApiException(ErrorCodes.ZoneNotFound, "Zone not found.", 404);
        return zone.ZoneId;
    }

    public async Task<List<SupplierDeliveryZoneDto>> GetBySupplierAsync(Guid? supplierToken, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var supplierId = await ResolveSupplierIdAsync(connection, supplierToken, context);

        var rows = (await connection.QueryAsync<SupplierDeliveryZone>(
            "sp_SupplierDeliveryZone_GetBySupplier", new { SupplierId = supplierId }, commandType: CommandType.StoredProcedure)).ToList();
        return mapper.MapList<SupplierDeliveryZoneDto>(rows);
    }

    public async Task<SupplierDeliveryZoneDto> CreateAsync(Guid? supplierToken, Guid zoneToken, int dayOfWeek, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var supplierId = await ResolveSupplierIdAsync(connection, supplierToken, context);
        var zoneId = await ResolveZoneIdAsync(connection, zoneToken);

        var p = new DynamicParameters();
        p.Add("@SupplierDeliveryZoneToken", Guid.NewGuid());
        p.Add("@SupplierId", supplierId);
        p.Add("@ZoneId", zoneId);
        p.Add("@DayOfWeek", (byte)dayOfWeek);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        var row = await connection.QueryFirstOrDefaultAsync<SupplierDeliveryZone>(
            "sp_SupplierDeliveryZone_Create", p, commandType: CommandType.StoredProcedure);

        return row is null
            ? throw new ApiException(ErrorCodes.SupplierDeliveryZoneNotFound, "Delivery zone coverage could not be created.", 500)
            : mapper.Map<SupplierDeliveryZoneDto>(row);
    }

    public async Task<bool> DeleteAsync(Guid? supplierToken, Guid zoneToken, int dayOfWeek, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var supplierId = await ResolveSupplierIdAsync(connection, supplierToken, context);
        var zoneId = await ResolveZoneIdAsync(connection, zoneToken);

        var p = new DynamicParameters();
        p.Add("@SupplierId", supplierId);
        p.Add("@ZoneId", zoneId);
        p.Add("@DayOfWeek", (byte)dayOfWeek);

        var deletedCount = await connection.ExecuteScalarAsync<int>(
            "sp_SupplierDeliveryZone_Delete", p, commandType: CommandType.StoredProcedure);

        return deletedCount > 0;
    }
}
