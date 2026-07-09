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

public class OrganizationService(IDbConnectionFactory connectionFactory, IMapper mapper) : IOrganizationService
{
    private sealed class OrganizationPageRow : Organization { public int TotalCount { get; set; } }

    private const int SuperAdminRoleLevel = 100;
    private const int AdminRoleLevel = 80;
    private const int ManagerRoleLevel = 60;

    private enum OrganizationScope { All, Hierarchy, Exact, None }

    // SuperAdmin: everything. Admin with no organization assigned: everything (treated like
    // SuperAdmin for organization scoping). Admin/Manager with an organization assigned: that
    // organization's subtree — the recursive hierarchy query naturally returns just the
    // organization itself when it has no children, so "parent sees children" / "child sees only
    // itself" fall out of the same query. Manager with no organization, or anyone below Manager:
    // exactly their own assigned organization, or nothing at all if they have none.
    private static (OrganizationScope Scope, int? OrganizationId) ResolveScope(IRequestContext context)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return (OrganizationScope.All, null);

        if (context.RoleLevel >= AdminRoleLevel)
            return context.OrganizationId.HasValue ? (OrganizationScope.Hierarchy, context.OrganizationId) : (OrganizationScope.All, null);

        if (context.RoleLevel >= ManagerRoleLevel)
            return context.OrganizationId.HasValue ? (OrganizationScope.Hierarchy, context.OrganizationId) : (OrganizationScope.None, null);

        return context.OrganizationId.HasValue ? (OrganizationScope.Exact, context.OrganizationId) : (OrganizationScope.None, null);
    }

    private static async Task<bool> CanManageAsync(IDbConnection connection, OrganizationScope scope, int? scopeOrganizationId, int targetOrganizationId, CancellationToken cancellationToken)
    {
        return scope switch
        {
            OrganizationScope.All => true,
            OrganizationScope.Exact => scopeOrganizationId == targetOrganizationId,
            OrganizationScope.Hierarchy => await connection.ExecuteScalarAsync<int>(
                "sp_Organization_IsInHierarchy",
                new { RootOrganizationId = scopeOrganizationId!.Value, TargetOrganizationId = targetOrganizationId },
                commandType: CommandType.StoredProcedure) == 1,
            _ => false
        };
    }

    public async Task<PagedResult<OrganizationDto>> GetOrganizationsAsync(
        int pageNumber,
        int pageSize,
        string? searchField,
        string? searchText,
        bool includeInactive,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        var (scope, scopeOrganizationId) = ResolveScope(context);
        if (scope == OrganizationScope.None)
            return new PagedResult<OrganizationDto>
            {
                Items = [],
                TotalCount = 0,
                PageNumber = safePageNumber,
                PageSize = safePageSize
            };

        await using var connection = connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", scope == OrganizationScope.Hierarchy ? scopeOrganizationId : (int?)null);
        p.Add("@ExactOrganizationId", scope == OrganizationScope.Exact ? scopeOrganizationId : (int?)null);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@IncludeInactive", includeInactive);

        var rows = (await connection.QueryAsync<OrganizationPageRow>(
            "sp_Organization_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<OrganizationDto>
        {
            Items = mapper.MapList<OrganizationDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<OrganizationDto?> GetOrganizationByTokenAsync(Guid organizationToken, IRequestContext context, CancellationToken cancellationToken)
    {
        var (scope, scopeOrganizationId) = ResolveScope(context);
        if (scope == OrganizationScope.None)
            return null;

        await using var connection = connectionFactory.CreateConnection();

        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken",
            new
            {
                OrganizationToken = organizationToken,
                RootOrganizationId = scope == OrganizationScope.Hierarchy ? scopeOrganizationId : (int?)null,
                ExactOrganizationId = scope == OrganizationScope.Exact ? scopeOrganizationId : (int?)null
            },
            commandType: CommandType.StoredProcedure);

        return organization is null ? null : mapper.Map<OrganizationDto>(organization);
    }

    public async Task<bool> OrganizationExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var result = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_ExistsByName",
            new { NormalizedName = name.ToUpperInvariant() },
            commandType: CommandType.StoredProcedure);

        return result == 1;
    }

    public async Task<OrganizationDto?> CreateOrganizationAsync(OrganizationDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        var (scope, scopeOrganizationId) = ResolveScope(context);
        if (scope == OrganizationScope.None)
            throw new ApiException(ErrorCodes.OrganizationCreateForbidden, "Not allowed to create organizations.", 403);

        await using var connection = connectionFactory.CreateConnection();

        // Creating a root organization (no parent) requires unrestricted scope; creating a child
        // requires the parent to be within the caller's manageable scope.
        var allowed = dto.ParentOrganizationId is null
            ? scope == OrganizationScope.All
            : await CanManageAsync(connection, scope, scopeOrganizationId, dto.ParentOrganizationId.Value, cancellationToken);

        if (!allowed)
            throw new ApiException(ErrorCodes.OrganizationParentOutsideScope, "Not allowed to create an organization under this parent.", 403);

        var created = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_Create",
            new
            {
                OrganizationToken = Guid.NewGuid(),
                Name = dto.Name,
                NormalizedName = dto.Name.ToUpperInvariant(),
                LegalName = dto.LegalName,
                Code = dto.Code,
                ParentOrganizationId = dto.ParentOrganizationId,
                OrganizationTypeId = dto.OrganizationTypeId == 0 ? (int?)null : dto.OrganizationTypeId,
                TimeZone = dto.TimeZone,
                CurrencyCode = dto.CurrencyCode,
                LanguageCode = dto.LanguageCode,
                IsActive = true,
                IsDeleted = false,
                CreatedUtc = DateTime.UtcNow,
                CreatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return created is null ? null : mapper.Map<OrganizationDto>(created);
    }

    public async Task<OrganizationDto?> EditOrganizationAsync(OrganizationDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken",
            new { OrganizationToken = dto.OrganizationToken, RootOrganizationId = (int?)null, ExactOrganizationId = (int?)null },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        var (scope, scopeOrganizationId) = ResolveScope(context);

        if (!await CanManageAsync(connection, scope, scopeOrganizationId, existing.OrganizationId, cancellationToken))
            throw new ApiException(ErrorCodes.OrganizationOutsideScope, "Cannot edit an organization outside your scope.", 403);

        var newParentOrganizationId = dto.ParentOrganizationId ?? existing.ParentOrganizationId;

        if (newParentOrganizationId.HasValue && newParentOrganizationId != existing.ParentOrganizationId
            && !await CanManageAsync(connection, scope, scopeOrganizationId, newParentOrganizationId.Value, cancellationToken))
            throw new ApiException(ErrorCodes.OrganizationParentOutsideScope, "Cannot move an organization under a parent outside your scope.", 403);

        var newName = !string.IsNullOrWhiteSpace(dto.Name) ? dto.Name : existing.Name;

        var updated = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_Update",
            new
            {
                OrganizationToken = dto.OrganizationToken,
                Name = newName,
                NormalizedName = newName.ToUpperInvariant(),
                LegalName = dto.LegalName ?? existing.LegalName,
                Code = dto.Code ?? existing.Code,
                ParentOrganizationId = newParentOrganizationId,
                OrganizationTypeId = dto.OrganizationTypeId == 0 ? (int?)null : dto.OrganizationTypeId,
                TimeZone = dto.TimeZone ?? existing.TimeZone,
                CurrencyCode = dto.CurrencyCode ?? existing.CurrencyCode,
                LanguageCode = dto.LanguageCode ?? existing.LanguageCode,
                LastUpdatedUtc = DateTime.UtcNow,
                LastUpdatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<OrganizationDto>(updated);
    }

    public async Task<bool> DeleteOrganizationAsync(Guid organizationToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken",
            new { OrganizationToken = organizationToken, RootOrganizationId = (int?)null, ExactOrganizationId = (int?)null },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return false;

        var (scope, scopeOrganizationId) = ResolveScope(context);

        if (!await CanManageAsync(connection, scope, scopeOrganizationId, existing.OrganizationId, cancellationToken))
            throw new ApiException(ErrorCodes.OrganizationDeleteForbidden, "Not allowed to delete this organization.", 403);

        await connection.ExecuteAsync(
            "sp_Organization_SoftDelete",
            new
            {
                OrganizationToken = organizationToken,
                DeletedUtc = DateTime.UtcNow,
                DeletedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return true;
    }
}
