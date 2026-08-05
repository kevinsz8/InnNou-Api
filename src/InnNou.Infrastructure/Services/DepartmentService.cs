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

public class DepartmentService(IDbConnectionFactory connectionFactory, IMapper mapper) : IDepartmentService
{
    private sealed class DepartmentPageRow : Department { public int TotalCount { get; set; } }

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    // Same shape as WarehouseService.CanManageOrganizationAsync — Department is structurally a
    // child entity scoped to one Organization, same as Warehouse/OrganizationContact.
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

    private static async Task<bool> ExistsByNormalizedNameAsync(IDbConnection connection, int organizationId, string normalizedName, Guid? excludeDepartmentToken = null)
    {
        var p = new DynamicParameters();
        p.Add("@OrganizationId", organizationId);
        p.Add("@NormalizedName", normalizedName);
        p.Add("@ExcludeDepartmentToken", excludeDepartmentToken);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_Department_ExistsByNormalizedName", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<PagedResult<DepartmentDto>> GetPagedByOrganizationTokenAsync(
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
            return new PagedResult<DepartmentDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

        var p = new DynamicParameters();
        p.Add("@OrganizationId", organization.OrganizationId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim());
        p.Add("@IncludeInactive", includeInactive);

        var rows = (await connection.QueryAsync<DepartmentPageRow>(
            "sp_Department_GetPagedByOrganizationId", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<DepartmentDto>
        {
            Items = mapper.MapList<DepartmentDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<DepartmentDto?> GetByTokenAsync(Guid departmentToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var department = await connection.QueryFirstOrDefaultAsync<Department>(
            "sp_Department_GetByToken", new { DepartmentToken = departmentToken }, commandType: CommandType.StoredProcedure);

        if (department is null || !await CanManageReadAsync(connection, context, department.OrganizationId))
            return null;

        return mapper.Map<DepartmentDto>(department);
    }

    public async Task<DepartmentDto?> CreateAsync(DepartmentDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken",
            new { OrganizationToken = dto.OrganizationToken, RootOrganizationId = (int?)null },
            commandType: CommandType.StoredProcedure);

        if (organization is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, organization.OrganizationId))
            throw new ApiException(ErrorCodes.DepartmentForbidden, "Insufficient permissions to create a department for this organization.", 403);

        var normalizedName = dto.Name.Trim().ToUpperInvariant();
        if (await ExistsByNormalizedNameAsync(connection, organization.OrganizationId, normalizedName))
            throw new ApiException(ErrorCodes.DepartmentNameExists, "A department with this name already exists in the organization.", 409);

        var p = new DynamicParameters();
        p.Add("@DepartmentToken", Guid.NewGuid());
        p.Add("@OrganizationId", organization.OrganizationId);
        p.Add("@Name", dto.Name.Trim());
        p.Add("@NormalizedName", normalizedName);
        p.Add("@Code", string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim());
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        var created = await connection.QueryFirstOrDefaultAsync<Department>(
            "sp_Department_Create", p, commandType: CommandType.StoredProcedure);

        return created is null ? null : mapper.Map<DepartmentDto>(created);
    }

    public async Task<DepartmentDto?> EditAsync(DepartmentDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Department>(
            "sp_Department_GetByToken", new { DepartmentToken = dto.DepartmentToken }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId))
            throw new ApiException(ErrorCodes.DepartmentOutsideScope, "Cannot edit a department from another organization.", 403);

        var normalizedName = dto.Name.Trim().ToUpperInvariant();
        if (await ExistsByNormalizedNameAsync(connection, existing.OrganizationId, normalizedName, dto.DepartmentToken))
            throw new ApiException(ErrorCodes.DepartmentNameExists, "A department with this name already exists in the organization.", 409);

        var p = new DynamicParameters();
        p.Add("@DepartmentToken", dto.DepartmentToken);
        p.Add("@Name", dto.Name.Trim());
        p.Add("@NormalizedName", normalizedName);
        p.Add("@Code", string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim());
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());

        var updated = await connection.QueryFirstOrDefaultAsync<Department>(
            "sp_Department_Update", p, commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<DepartmentDto>(updated);
    }

    public async Task<DepartmentDto?> SetActiveAsync(Guid departmentToken, bool isActive, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Department>(
            "sp_Department_GetByToken", new { DepartmentToken = departmentToken }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId))
            throw new ApiException(ErrorCodes.DepartmentOutsideScope, "Cannot change the active state of a department from another organization.", 403);

        var p = new DynamicParameters();
        p.Add("@DepartmentToken", departmentToken);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());

        var updated = await connection.QueryFirstOrDefaultAsync<Department>(
            "sp_Department_SetActive", p, commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<DepartmentDto>(updated);
    }
}
