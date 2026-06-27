using Dapper;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class UnitConversionRateService(IDbConnectionFactory connectionFactory, IMapper mapper) : IUnitConversionRateService
{
    public async Task<List<UnitConversionRateDto>> GetAllAsync(int? unitTypeId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitTypeId", unitTypeId);
        var rows = (await connection.QueryAsync<UnitConversionRate>(
            "sp_UnitConversionRate_GetAll", p, commandType: CommandType.StoredProcedure)).ToList();
        return mapper.MapList<UnitConversionRateDto>(rows);
    }

    public async Task<UnitConversionRateDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitConversionRateToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
            "sp_UnitConversionRate_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitConversionRateDto>(row);
    }

    public async Task<UnitConversionRateDto?> CreateAsync(UnitConversionRateDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@FromUnitOfMeasureId", dto.FromUnitOfMeasureId);
        p.Add("@ToUnitOfMeasureId", dto.ToUnitOfMeasureId);
        p.Add("@Factor", dto.Factor);
        p.Add("@CreatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
            "sp_UnitConversionRate_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitConversionRateDto>(row);
    }

    public async Task<UnitConversionRateDto?> EditAsync(UnitConversionRateDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitConversionRateToken", dto.UnitConversionRateToken);
        p.Add("@Factor", dto.Factor);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
            "sp_UnitConversionRate_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitConversionRateDto>(row);
    }

    public async Task<UnitConversionRateDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitConversionRateToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
            "sp_UnitConversionRate_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitConversionRateDto>(row);
    }
}
