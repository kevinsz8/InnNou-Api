using Dapper;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class UnitOfMeasureService(IDbConnectionFactory connectionFactory, IMapper mapper) : IUnitOfMeasureService
{
    private sealed class UnitOfMeasurePageRow : UnitOfMeasure { public int TotalCount { get; set; } }

    public async Task<PagedResult<UnitOfMeasureDto>> GetPagedAsync(int pageNumber, int pageSize, int? unitTypeId = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@UnitTypeId", unitTypeId);
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<UnitOfMeasurePageRow>(
            "sp_UnitOfMeasure_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();
        return new PagedResult<UnitOfMeasureDto>
        {
            Items = mapper.MapList<UnitOfMeasureDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<UnitOfMeasureDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitOfMeasureToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
            "sp_UnitOfMeasure_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitOfMeasureDto>(row);
    }

    public async Task<bool> ExistsByCodeAsync(string code, int unitTypeId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", code);
        p.Add("@UnitTypeId", unitTypeId);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_UnitOfMeasure_ExistsByCode", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<UnitOfMeasureDto?> CreateAsync(UnitOfMeasureDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitOfMeasureToken", Guid.NewGuid());
        p.Add("@UnitTypeId", dto.UnitTypeId);
        p.Add("@Code", dto.Code);
        p.Add("@Symbol", dto.Symbol);
        p.Add("@Decimals", dto.Decimals);
        p.Add("@CreatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
            "sp_UnitOfMeasure_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitOfMeasureDto>(row);
    }

    public async Task<UnitOfMeasureDto?> EditAsync(UnitOfMeasureDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitOfMeasureToken", dto.UnitOfMeasureToken);
        p.Add("@Code", dto.Code);
        p.Add("@Symbol", dto.Symbol);
        p.Add("@Decimals", dto.Decimals);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
            "sp_UnitOfMeasure_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitOfMeasureDto>(row);
    }

    public async Task<UnitOfMeasureDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitOfMeasureToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
            "sp_UnitOfMeasure_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitOfMeasureDto>(row);
    }
}
