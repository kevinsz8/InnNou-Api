using AutoMapper;
using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class RoleService(IDbConnectionFactory connectionFactory, IMapper mapper) : IRoleService
{
    public async Task<PagedResult<RoleDto>> GetRolesAsync(
        int pageNumber,
        int pageSize,
        string? searchField,
        string? searchText,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@MaxLevel", context.RoleLevel);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var roles = (await connection.QueryAsync<Role>(
            "sp_Role_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<RoleDto>
        {
            Items = mapper.Map<List<RoleDto>>(roles),
            TotalCount = p.Get<int>("@TotalCount"),
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }
}
