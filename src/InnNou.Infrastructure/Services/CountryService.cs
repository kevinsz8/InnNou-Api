using Dapper;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class CountryService(IDbConnectionFactory connectionFactory, IMapper mapper) : ICountryService
{
    public async Task<List<CountryDto>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<Country>(
            "sp_Country_GetAll", p, commandType: CommandType.StoredProcedure)).ToList();
        return mapper.MapList<CountryDto>(rows);
    }
}
