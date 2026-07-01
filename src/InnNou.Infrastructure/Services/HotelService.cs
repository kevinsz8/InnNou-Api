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

public class HotelService(IDbConnectionFactory connectionFactory, IMapper mapper) : IHotelService
{
    private sealed class HotelPageRow : Hotel { public int TotalCount { get; set; } }

    private const int SuperAdminRoleLevel = 100;
    private const int AdminRoleLevel = 80;
    private const int ManagerRoleLevel = 60;

    private enum HotelScope { All, Hierarchy, Exact, None }

    // SuperAdmin: everything. Admin with no hotel assigned: everything (treated like SuperAdmin
    // for hotel scoping). Admin/Manager with a hotel assigned: that hotel's subtree — the
    // recursive hierarchy query naturally returns just the hotel itself when it has no children,
    // so "parent sees children" / "child sees only itself" fall out of the same query. Manager
    // with no hotel, or anyone below Manager: exactly their own assigned hotel, or nothing at all
    // if they have none.
    private static (HotelScope Scope, int? HotelId) ResolveScope(IRequestContext context)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return (HotelScope.All, null);

        if (context.RoleLevel >= AdminRoleLevel)
            return context.HotelId.HasValue ? (HotelScope.Hierarchy, context.HotelId) : (HotelScope.All, null);

        if (context.RoleLevel >= ManagerRoleLevel)
            return context.HotelId.HasValue ? (HotelScope.Hierarchy, context.HotelId) : (HotelScope.None, null);

        return context.HotelId.HasValue ? (HotelScope.Exact, context.HotelId) : (HotelScope.None, null);
    }

    private static async Task<bool> CanManageAsync(IDbConnection connection, HotelScope scope, int? scopeHotelId, int targetHotelId, CancellationToken cancellationToken)
    {
        return scope switch
        {
            HotelScope.All => true,
            HotelScope.Exact => scopeHotelId == targetHotelId,
            HotelScope.Hierarchy => await connection.ExecuteScalarAsync<int>(
                "sp_Hotel_IsInHierarchy",
                new { RootHotelId = scopeHotelId!.Value, TargetHotelId = targetHotelId },
                commandType: CommandType.StoredProcedure) == 1,
            _ => false
        };
    }

    public async Task<PagedResult<HotelDto>> GetHotelsAsync(
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

        var (scope, scopeHotelId) = ResolveScope(context);
        if (scope == HotelScope.None)
            return new PagedResult<HotelDto>
            {
                Items = [],
                TotalCount = 0,
                PageNumber = safePageNumber,
                PageSize = safePageSize
            };

        await using var connection = connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@RootHotelId", scope == HotelScope.Hierarchy ? scopeHotelId : (int?)null);
        p.Add("@ExactHotelId", scope == HotelScope.Exact ? scopeHotelId : (int?)null);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@IncludeInactive", includeInactive);

        var rows = (await connection.QueryAsync<HotelPageRow>(
            "sp_Hotel_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<HotelDto>
        {
            Items = mapper.MapList<HotelDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<HotelDto?> GetHotelByTokenAsync(Guid hotelToken, IRequestContext context, CancellationToken cancellationToken)
    {
        var (scope, scopeHotelId) = ResolveScope(context);
        if (scope == HotelScope.None)
            return null;

        await using var connection = connectionFactory.CreateConnection();

        var hotel = await connection.QueryFirstOrDefaultAsync<Hotel>(
            "sp_Hotel_GetByToken",
            new
            {
                HotelToken = hotelToken,
                RootHotelId = scope == HotelScope.Hierarchy ? scopeHotelId : (int?)null,
                ExactHotelId = scope == HotelScope.Exact ? scopeHotelId : (int?)null
            },
            commandType: CommandType.StoredProcedure);

        return hotel is null ? null : mapper.Map<HotelDto>(hotel);
    }

    public async Task<bool> HotelExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var result = await connection.ExecuteScalarAsync<int>(
            "sp_Hotel_ExistsByName",
            new { NormalizedName = name.ToUpperInvariant() },
            commandType: CommandType.StoredProcedure);

        return result == 1;
    }

    public async Task<HotelDto?> CreateHotelAsync(HotelDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        var (scope, scopeHotelId) = ResolveScope(context);
        if (scope == HotelScope.None)
            throw new UnauthorizedAccessException("Not allowed to create hotels.");

        await using var connection = connectionFactory.CreateConnection();

        // Creating a root hotel (no parent) requires unrestricted scope; creating a child
        // requires the parent to be within the caller's manageable scope.
        var allowed = dto.ParentHotelId is null
            ? scope == HotelScope.All
            : await CanManageAsync(connection, scope, scopeHotelId, dto.ParentHotelId.Value, cancellationToken);

        if (!allowed)
            throw new UnauthorizedAccessException("Not allowed to create a hotel under this parent.");

        var created = await connection.QueryFirstOrDefaultAsync<Hotel>(
            "sp_Hotel_Create",
            new
            {
                HotelToken = Guid.NewGuid(),
                Name = dto.Name,
                NormalizedName = dto.Name.ToUpperInvariant(),
                LegalName = dto.LegalName,
                Code = dto.Code,
                ParentHotelId = dto.ParentHotelId,
                TimeZone = dto.TimeZone,
                CurrencyCode = dto.CurrencyCode,
                LanguageCode = dto.LanguageCode,
                IsActive = true,
                IsDeleted = false,
                CreatedUtc = DateTime.UtcNow,
                CreatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return created is null ? null : mapper.Map<HotelDto>(created);
    }

    public async Task<HotelDto?> EditHotelAsync(HotelDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Hotel>(
            "sp_Hotel_GetByToken",
            new { HotelToken = dto.HotelToken, RootHotelId = (int?)null, ExactHotelId = (int?)null },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        var (scope, scopeHotelId) = ResolveScope(context);

        if (!await CanManageAsync(connection, scope, scopeHotelId, existing.HotelId, cancellationToken))
            throw new UnauthorizedAccessException("Cannot edit a hotel outside your scope.");

        var newParentHotelId = dto.ParentHotelId ?? existing.ParentHotelId;

        if (newParentHotelId.HasValue && newParentHotelId != existing.ParentHotelId
            && !await CanManageAsync(connection, scope, scopeHotelId, newParentHotelId.Value, cancellationToken))
            throw new UnauthorizedAccessException("Cannot move a hotel under a parent outside your scope.");

        var newName = !string.IsNullOrWhiteSpace(dto.Name) ? dto.Name : existing.Name;

        var updated = await connection.QueryFirstOrDefaultAsync<Hotel>(
            "sp_Hotel_Update",
            new
            {
                HotelToken = dto.HotelToken,
                Name = newName,
                NormalizedName = newName.ToUpperInvariant(),
                LegalName = dto.LegalName ?? existing.LegalName,
                Code = dto.Code ?? existing.Code,
                ParentHotelId = newParentHotelId,
                TimeZone = dto.TimeZone ?? existing.TimeZone,
                CurrencyCode = dto.CurrencyCode ?? existing.CurrencyCode,
                LanguageCode = dto.LanguageCode ?? existing.LanguageCode,
                LastUpdatedUtc = DateTime.UtcNow,
                LastUpdatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<HotelDto>(updated);
    }

    public async Task<bool> DeleteHotelAsync(Guid hotelToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Hotel>(
            "sp_Hotel_GetByToken",
            new { HotelToken = hotelToken, RootHotelId = (int?)null, ExactHotelId = (int?)null },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return false;

        var (scope, scopeHotelId) = ResolveScope(context);

        if (!await CanManageAsync(connection, scope, scopeHotelId, existing.HotelId, cancellationToken))
            throw new UnauthorizedAccessException("Not allowed to delete this hotel.");

        await connection.ExecuteAsync(
            "sp_Hotel_SoftDelete",
            new
            {
                HotelToken = hotelToken,
                DeletedUtc = DateTime.UtcNow,
                DeletedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return true;
    }
}
