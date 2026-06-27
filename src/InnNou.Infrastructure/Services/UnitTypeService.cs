using Dapper;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class UnitTypeService(IDbConnectionFactory connectionFactory, IMapper mapper) : IUnitTypeService
{
    public async Task<List<UnitTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<UnitType>(
            "sp_UnitType_GetAll", commandType: CommandType.StoredProcedure)).ToList();
        return mapper.MapList<UnitTypeDto>(rows);
    }

    public async Task<UnitTypeDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitTypeToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<UnitType>(
            "sp_UnitType_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitTypeDto>(row);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", code);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_UnitType_ExistsByCode", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<UnitTypeDto?> CreateAsync(UnitTypeDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", dto.Code);
        p.Add("@CreatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<UnitType>(
            "sp_UnitType_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitTypeDto>(row);
    }

    public async Task<UnitTypeDto?> EditAsync(UnitTypeDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitTypeToken", dto.UnitTypeToken);
        p.Add("@Code", dto.Code);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<UnitType>(
            "sp_UnitType_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitTypeDto>(row);
    }

    public async Task<UnitTypeDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitTypeToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<UnitType>(
            "sp_UnitType_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitTypeDto>(row);
    }
}
