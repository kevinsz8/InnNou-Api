using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using Microsoft.Data.SqlClient;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class WarehouseService(IDbConnectionFactory connectionFactory, IMapper mapper) : IWarehouseService
{
    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    private sealed class WarehousePageRow : Warehouse { public int TotalCount { get; set; } }

    // RoleLevel >= 100 manages any organization. RoleLevel >= 20 (Staff/Admin) manages only
    // within their own organization's hierarchy (root-or-descendant). Below that, no access.
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

    private static async Task<bool> ExistsByNormalizedNameAsync(IDbConnection connection, int organizationId, string normalizedName, Guid? excludeWarehouseToken = null)
    {
        var p = new DynamicParameters();
        p.Add("@OrganizationId", organizationId);
        p.Add("@NormalizedName", normalizedName);
        p.Add("@ExcludeWarehouseToken", excludeWarehouseToken);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_Warehouse_ExistsByNormalizedName", p, commandType: CommandType.StoredProcedure);
    }

    private static void AddCapabilityParameters(DynamicParameters p, WarehouseDto dto)
    {
        p.Add("@IsInventoriable", dto.IsInventoriable);
        p.Add("@CanReceivePurchases", dto.CanReceivePurchases);
        p.Add("@CanReceiveTransfers", dto.CanReceiveTransfers);
        p.Add("@CanTransferOut", dto.CanTransferOut);
        p.Add("@CanConsumeInventory", dto.CanConsumeInventory);
        p.Add("@CanProduceItems", dto.CanProduceItems);
        p.Add("@CanSellItems", dto.CanSellItems);
        p.Add("@CanAdjustInventory", dto.CanAdjustInventory);
        p.Add("@CanReceiveReturns", dto.CanReceiveReturns);
        p.Add("@TrackLotNumbers", dto.TrackLotNumbers);
        p.Add("@TrackExpirationDates", dto.TrackExpirationDates);
        p.Add("@TrackSerialNumbers", dto.TrackSerialNumbers);
        p.Add("@RequireApproval", dto.RequireApproval);
        p.Add("@IsDefaultReceivingWarehouse", dto.IsDefaultReceivingWarehouse);
        p.Add("@IsDefaultConsumptionWarehouse", dto.IsDefaultConsumptionWarehouse);
    }

    public async Task<PagedResult<WarehouseDto>> GetPagedByOrganizationTokenAsync(
        Guid organizationToken,
        int pageNumber,
        int pageSize,
        string? searchText,
        bool includeInactive,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken",
            new { OrganizationToken = organizationToken, RootOrganizationId = (int?)null },
            commandType: CommandType.StoredProcedure);

        if (organization is null || !await CanManageReadAsync(connection, context, organization.OrganizationId))
            return new PagedResult<WarehouseDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

        var p = new DynamicParameters();
        p.Add("@OrganizationId", organization.OrganizationId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim());
        p.Add("@IncludeInactive", includeInactive);

        var rows = (await connection.QueryAsync<WarehousePageRow>(
            "sp_Warehouse_GetPagedByOrganizationId", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<WarehouseDto>
        {
            Items = mapper.MapList<WarehouseDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<WarehouseDto?> GetByTokenAsync(Guid warehouseToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);

        if (warehouse is null || !await CanManageReadAsync(connection, context, warehouse.OrganizationId))
            return null;

        return mapper.Map<WarehouseDto>(warehouse);
    }

    // Reads are scoped by hierarchy the same way writes are, but without the StaffRoleLevel
    // floor — any organization-scoped caller may view their own org's warehouses.
    private static async Task<bool> CanManageReadAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
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

    public async Task<WarehouseDto?> CreateAsync(WarehouseDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken",
            new { OrganizationToken = dto.OrganizationToken, RootOrganizationId = (int?)null },
            commandType: CommandType.StoredProcedure);

        if (organization is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, organization.OrganizationId))
            throw new ApiException(ErrorCodes.WarehouseForbidden, "Insufficient permissions to create a warehouse for this organization.", 403);

        var normalizedName = dto.Name.Trim().ToUpperInvariant();
        if (await ExistsByNormalizedNameAsync(connection, organization.OrganizationId, normalizedName))
            throw new ApiException(ErrorCodes.WarehouseNameExists, "A warehouse with this name already exists in the organization.", 409);

        var p = new DynamicParameters();
        p.Add("@WarehouseToken", Guid.NewGuid());
        p.Add("@OrganizationId", organization.OrganizationId);
        p.Add("@Name", dto.Name.Trim());
        p.Add("@NormalizedName", normalizedName);
        p.Add("@Code", string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim());
        p.Add("@Description", dto.Description);
        p.Add("@PurposeCode", dto.PurposeCode);
        AddCapabilityParameters(p, dto);
        p.Add("@CreatedUtc", DateTime.UtcNow);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        try
        {
            var created = await connection.QueryFirstOrDefaultAsync<Warehouse>(
                "sp_Warehouse_Create", p, commandType: CommandType.StoredProcedure);
            return created is null ? null : mapper.Map<WarehouseDto>(created);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw TranslateUniqueViolation(ex);
        }
    }

    public async Task<WarehouseDto?> EditAsync(WarehouseDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = dto.WarehouseToken }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId))
            throw new ApiException(ErrorCodes.WarehouseOutsideScope, "Cannot edit a warehouse from another organization.", 403);

        var normalizedName = dto.Name.Trim().ToUpperInvariant();
        if (await ExistsByNormalizedNameAsync(connection, existing.OrganizationId, normalizedName, dto.WarehouseToken))
            throw new ApiException(ErrorCodes.WarehouseNameExists, "A warehouse with this name already exists in the organization.", 409);

        var p = new DynamicParameters();
        p.Add("@WarehouseToken", dto.WarehouseToken);
        p.Add("@Name", dto.Name.Trim());
        p.Add("@NormalizedName", normalizedName);
        p.Add("@Code", string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim());
        p.Add("@Description", dto.Description);
        p.Add("@PurposeCode", dto.PurposeCode);
        AddCapabilityParameters(p, dto);
        p.Add("@LastUpdatedUtc", DateTime.UtcNow);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());

        try
        {
            var updated = await connection.QueryFirstOrDefaultAsync<Warehouse>(
                "sp_Warehouse_Update", p, commandType: CommandType.StoredProcedure);
            return updated is null ? null : mapper.Map<WarehouseDto>(updated);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw TranslateUniqueViolation(ex);
        }
    }

    public async Task<WarehouseDto?> SetActiveAsync(Guid warehouseToken, bool isActive, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId))
            throw new ApiException(ErrorCodes.WarehouseOutsideScope, "Cannot change the active state of a warehouse from another organization.", 403);

        var p = new DynamicParameters();
        p.Add("@WarehouseToken", warehouseToken);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedUtc", DateTime.UtcNow);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());

        var updated = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_SetActive", p, commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<WarehouseDto>(updated);
    }

    public async Task<bool> DeleteAsync(Guid warehouseToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return false;

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId))
            throw new ApiException(ErrorCodes.WarehouseOutsideScope, "Cannot delete a warehouse from another organization.", 403);

        var now = DateTime.UtcNow;
        var actor = context.ActorUserToken.ToString();

        await connection.ExecuteAsync(
            "sp_Warehouse_SoftDelete",
            new
            {
                WarehouseToken = warehouseToken,
                DeletedUtc = now,
                DeletedBy = actor,
                LastUpdatedUtc = now,
                LastUpdatedBy = actor
            },
            commandType: CommandType.StoredProcedure);

        return true;
    }

    // Parses the violated index name out of the SQL Server error message rather than guessing
    // from the submitted dto — the message names the exact index (e.g. "...unique index
    // 'UX_Warehouses_DefaultReceiving'"), so this is precise even when multiple constraints
    // could plausibly apply (e.g. a row that is both a duplicate name AND a duplicate default).
    private static ApiException TranslateUniqueViolation(SqlException ex)
    {
        if (ex.Message.Contains("UX_Warehouses_DefaultReceiving", StringComparison.OrdinalIgnoreCase))
            return new ApiException(ErrorCodes.WarehouseDefaultReceivingConflict, "Another warehouse in this organization is already the default receiving warehouse.", 409);

        if (ex.Message.Contains("UX_Warehouses_DefaultConsumption", StringComparison.OrdinalIgnoreCase))
            return new ApiException(ErrorCodes.WarehouseDefaultConsumptionConflict, "Another warehouse in this organization is already the default consumption warehouse.", 409);

        if (ex.Message.Contains("UX_Warehouses_Code_NotDeleted", StringComparison.OrdinalIgnoreCase))
            return new ApiException(ErrorCodes.WarehouseNameExists, "A warehouse with this code already exists in the organization.", 409);

        return new ApiException(ErrorCodes.WarehouseNameExists, "A warehouse with this name already exists in the organization.", 409);
    }
}
