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

public class ZoneService(IDbConnectionFactory connectionFactory, IMapper mapper) : IZoneService
{
    private sealed class ZonePageRow : Zone { public int TotalCount { get; set; } }

    private const int AdminRoleLevel = 80;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<ZoneDto>> GetPagedAsync(int pageNumber, int pageSize, string? countryCode, string? searchText, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@CountryCode", string.IsNullOrWhiteSpace(countryCode) ? null : countryCode.Trim().ToUpperInvariant());
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim());
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<ZonePageRow>(
            "sp_Zone_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();
        return new PagedResult<ZoneDto>
        {
            Items = mapper.MapList<ZoneDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<ZoneDto?> GetByTokenAsync(Guid zoneToken, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<Zone>(
            "sp_Zone_GetByToken", new { ZoneToken = zoneToken }, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<ZoneDto>(row);
    }

    public async Task<ZoneDto?> CreateAsync(ZoneDto dto, string countryCode, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.ZoneForbidden, "Insufficient permissions to create a zone.", 403);

        await using var connection = connectionFactory.CreateConnection();

        var country = await connection.QueryFirstOrDefaultAsync<Country>(
            "sp_Country_GetByCode", new { Code = countryCode.Trim().ToUpperInvariant() }, commandType: CommandType.StoredProcedure);
        if (country is null)
            throw new ApiException(ErrorCodes.CountryNotFound, "Country not found.", 404);

        var exists = await connection.ExecuteScalarAsync<bool>(
            "sp_Zone_ExistsByCode", new { country.CountryId, dto.Code, ExcludeZoneId = (int?)null }, commandType: CommandType.StoredProcedure);
        if (exists)
            throw new ApiException(ErrorCodes.ZoneCodeExists, "A zone with this code already exists in this country.", 409);

        var p = new DynamicParameters();
        p.Add("@ZoneToken", Guid.NewGuid());
        p.Add("@CountryId", country.CountryId);
        p.Add("@Code", dto.Code);
        p.Add("@Name", dto.Name);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<Zone>(
            "sp_Zone_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<ZoneDto>(row);
    }

    public async Task<ZoneDto?> EditAsync(ZoneDto dto, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.ZoneForbidden, "Insufficient permissions to edit a zone.", 403);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ZoneToken", dto.ZoneToken);
        p.Add("@Code", dto.Code);
        p.Add("@Name", dto.Name);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<Zone>(
            "sp_Zone_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<ZoneDto>(row);
    }

    public async Task<ZoneDto?> SetActiveAsync(Guid zoneToken, bool isActive, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.ZoneForbidden, "Insufficient permissions to change a zone's active state.", 403);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ZoneToken", zoneToken);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<Zone>(
            "sp_Zone_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<ZoneDto>(row);
    }
}
