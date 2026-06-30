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

public class RoleService(IDbConnectionFactory connectionFactory, IMapper mapper) : IRoleService
{
    private sealed class RolePageRow : Role { public int TotalCount { get; set; } }

    public async Task<RoleDto?> GetRoleByTokenAsync(Guid roleToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var role = await connection.QueryFirstOrDefaultAsync<Role>(
            "sp_Role_GetByToken",
            new { RoleToken = roleToken, MaxLevel = context.RoleLevel },
            commandType: CommandType.StoredProcedure);

        return role is null ? null : mapper.Map<RoleDto>(role);
    }

    public async Task<PagedResult<RoleDto>> GetRolesAsync(
        int pageNumber,
        int pageSize,
        string? searchField,
        string? searchText,
        bool includeInactive,
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
        p.Add("@IncludeInactive", includeInactive);

        var rows = (await connection.QueryAsync<RolePageRow>(
            "sp_Role_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<RoleDto>
        {
            Items = mapper.MapList<RoleDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }
}
