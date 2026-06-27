using Dapper;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class UnitOfMeasureService(IDbConnectionFactory connectionFactory, IMapper mapper) : IUnitOfMeasureService
{
    public async Task<List<UnitOfMeasureDto>> GetAllAsync(int? unitTypeId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitTypeId", unitTypeId);
        var rows = (await connection.QueryAsync<UnitOfMeasure>(
            "sp_UnitOfMeasure_GetAll", p, commandType: CommandType.StoredProcedure)).ToList();
        return mapper.MapList<UnitOfMeasureDto>(rows);
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
