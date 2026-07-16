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

public class ArticleFavoriteService(IDbConnectionFactory connectionFactory, IMapper mapper) : IArticleFavoriteService
{
    private sealed class ArticleFavoritePageRow : ArticleFavorite { public int TotalCount { get; set; } }

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    // Create/Delete always act on the caller's own organization — cascading to descendants is
    // automatic on the read side (ancestor CTE in sp_ArticleFavorite_GetEffective), never
    // something a parent "does to" a specific child, so there's no OrganizationToken input
    // here. Gated at Staff+ (RoleLevel >= 20), the same floor WarehouseService/
    // OrganizationContactService use for org-scoped writes.
    private static int RequireOwnOrganizationForWrite(IRequestContext context)
    {
        if (!context.OrganizationId.HasValue)
            throw new ApiException(ErrorCodes.ArticleFavoriteNoOrganizationContext,
                "This action requires an organization-scoped account.", 403);
        if (context.RoleLevel < StaffRoleLevel)
            throw new ApiException(ErrorCodes.ArticleFavoriteForbidden,
                "Insufficient permissions to manage this organization's favorites.", 403);
        return context.OrganizationId.Value;
    }

    // Reads mirror WarehouseService.CanManageReadAsync — SuperAdmin unrestricted, everyone else
    // may read only within their own organization's hierarchy (root-or-descendant), no
    // RoleLevel floor (viewing your own org's favorites is not a sensitive action).
    private static async Task<bool> CanReadOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel) return true;
        if (!context.OrganizationId.HasValue) return false;
        if (context.OrganizationId.Value == targetOrganizationId) return true;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    public async Task<ArticleFavoriteDto> CreateAsync(int articleId, IRequestContext context, CancellationToken cancellationToken = default)
    {
        var organizationId = RequireOwnOrganizationForWrite(context);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ArticleFavoriteToken", Guid.NewGuid());
        p.Add("@ArticleId", articleId);
        p.Add("@OrganizationId", organizationId);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        var row = await connection.QueryFirstOrDefaultAsync<ArticleFavorite>(
            "sp_ArticleFavorite_Create", p, commandType: CommandType.StoredProcedure);

        if (row is null)
            throw new ApiException(ErrorCodes.ArticleFavoriteCreateFailed, "Article favorite could not be created.", 500);

        return mapper.Map<ArticleFavoriteDto>(row);
    }

    public async Task<bool> DeleteAsync(int articleId, IRequestContext context, CancellationToken cancellationToken = default)
    {
        var organizationId = RequireOwnOrganizationForWrite(context);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ArticleId", articleId);
        p.Add("@OrganizationId", organizationId);

        var deletedCount = await connection.ExecuteScalarAsync<int>(
            "sp_ArticleFavorite_Delete", p, commandType: CommandType.StoredProcedure);

        return deletedCount > 0;
    }

    public async Task<PagedResult<ArticleFavoriteDto>> GetEffectiveAsync(int pageNumber, int pageSize, Guid? organizationToken, string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        int organizationId;
        if (organizationToken.HasValue)
        {
            var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                "sp_Organization_GetByToken",
                new { OrganizationToken = organizationToken.Value, RootOrganizationId = (int?)null },
                commandType: CommandType.StoredProcedure);

            if (organization is null || !await CanReadOrganizationAsync(connection, context, organization.OrganizationId))
                return new PagedResult<ArticleFavoriteDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            organizationId = organization.OrganizationId;
        }
        else
        {
            if (!context.OrganizationId.HasValue)
                throw new ApiException(ErrorCodes.ArticleFavoriteNoOrganizationContext, "This action requires an organization-scoped account.", 403);
            organizationId = context.OrganizationId.Value;
        }

        var p = new DynamicParameters();
        p.Add("@OrganizationId", organizationId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim());
        p.Add("@IncludeInactive", includeInactive);

        var rows = (await connection.QueryAsync<ArticleFavoritePageRow>(
            "sp_ArticleFavorite_GetEffective", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<ArticleFavoriteDto>
        {
            Items = mapper.MapList<ArticleFavoriteDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }
}
