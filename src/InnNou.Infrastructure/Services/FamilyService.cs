using Dapper;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class FamilyService(IDbConnectionFactory connectionFactory, IMapper mapper) : IFamilyService
{
    private sealed class FamilyPageRow : Family { public int TotalCount { get; set; } }

    public async Task<PagedResult<FamilyDto>> GetPagedAsync(int pageNumber, int pageSize, string? searchText = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<FamilyPageRow>(
            "sp_Family_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();
        return new PagedResult<FamilyDto>
        {
            Items = mapper.MapList<FamilyDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<FamilyDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@FamilyToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<Family>(
            "sp_Family_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<FamilyDto>(row);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", code);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_Family_ExistsByCode", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<FamilyDto?> CreateAsync(FamilyDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", dto.Code);
        p.Add("@CreatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<Family>(
            "sp_Family_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<FamilyDto>(row);
    }

    public async Task<FamilyDto?> EditAsync(FamilyDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@FamilyToken", dto.FamilyToken);
        p.Add("@Code", dto.Code);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<Family>(
            "sp_Family_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<FamilyDto>(row);
    }

    public async Task<FamilyDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@FamilyToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<Family>(
            "sp_Family_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<FamilyDto>(row);
    }
}
