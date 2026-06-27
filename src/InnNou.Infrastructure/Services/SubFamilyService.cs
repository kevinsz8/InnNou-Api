using Dapper;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class SubFamilyService(IDbConnectionFactory connectionFactory, IMapper mapper) : ISubFamilyService
{
    private sealed class SubFamilyPageRow : SubFamily { public int TotalCount { get; set; } }

    public async Task<PagedResult<SubFamilyDto>> GetPagedAsync(int pageNumber, int pageSize, int? familyId = null, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@FamilyId", familyId);
        var rows = (await connection.QueryAsync<SubFamilyPageRow>(
            "sp_SubFamily_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();
        return new PagedResult<SubFamilyDto>
        {
            Items = mapper.MapList<SubFamilyDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<SubFamilyDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubFamilyToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<SubFamily>(
            "sp_SubFamily_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubFamilyDto>(row);
    }

    public async Task<bool> ExistsByCodeAsync(string code, int familyId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", code);
        p.Add("@FamilyId", familyId);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_SubFamily_ExistsByCode", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<SubFamilyDto?> CreateAsync(SubFamilyDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@FamilyId", dto.FamilyId);
        p.Add("@Code", dto.Code);
        p.Add("@CreatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<SubFamily>(
            "sp_SubFamily_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubFamilyDto>(row);
    }

    public async Task<SubFamilyDto?> EditAsync(SubFamilyDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubFamilyToken", dto.SubFamilyToken);
        p.Add("@Code", dto.Code);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<SubFamily>(
            "sp_SubFamily_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubFamilyDto>(row);
    }

    public async Task<SubFamilyDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubFamilyToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<SubFamily>(
            "sp_SubFamily_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubFamilyDto>(row);
    }
}
