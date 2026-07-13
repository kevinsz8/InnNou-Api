using Dapper;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class MenuService(IDbConnectionFactory connectionFactory) : IMenuService
{
    public async Task<List<MenuItemDto>> GetVisibleForContextAsync(int roleLevel, int? organizationId, int? supplierId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@RoleLevel", roleLevel);
        p.Add("@OrganizationId", organizationId);
        p.Add("@SupplierId", supplierId);
        var rows = (await connection.QueryAsync<MenuItem>(
            "sp_MenuItem_GetVisibleForContext", p, commandType: CommandType.StoredProcedure)).ToList();

        // ParentMenuItemId never crosses the wire (only tokens do) — resolved here via an
        // in-memory lookup over the same flat result set rather than a SQL self-join.
        var tokensById = rows.ToDictionary(r => r.MenuItemId, r => r.MenuItemToken);

        return rows.Select(r => new MenuItemDto
        {
            MenuItemId = r.MenuItemId,
            MenuItemToken = r.MenuItemToken,
            ParentMenuItemToken = r.ParentMenuItemId.HasValue && tokensById.TryGetValue(r.ParentMenuItemId.Value, out var parentToken)
                ? parentToken
                : null,
            Name = r.Name,
            Route = r.Route,
            Icon = r.Icon,
            SortOrder = r.SortOrder,
        }).ToList();
    }
}
