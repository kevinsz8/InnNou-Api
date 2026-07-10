using Dapper;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class CurrencyService(IDbConnectionFactory connectionFactory, IMapper mapper) : ICurrencyService
{
    public async Task<List<CurrencyDto>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<Currency>(
            "sp_Currency_GetAll", p, commandType: CommandType.StoredProcedure)).ToList();
        return mapper.MapList<CurrencyDto>(rows);
    }

    public async Task<bool> ExistsActiveByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", code);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_Currency_ExistsByCode", p, commandType: CommandType.StoredProcedure);
    }
}
