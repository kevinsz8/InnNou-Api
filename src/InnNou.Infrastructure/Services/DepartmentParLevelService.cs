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

public class DepartmentParLevelService(IDbConnectionFactory connectionFactory, IMapper mapper) : IDepartmentParLevelService
{
    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    // Same shape as DepartmentService.CanManageOrganizationAsync — a DepartmentParLevel is scoped
    // to one Department, which is itself scoped to one Organization; no Warehouse/WarehouseScopeGuard
    // concern here at all, since the configuration doesn't depend on which Warehouse fulfills it.
    private static async Task<bool> CanManageOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (context.RoleLevel < StaffRoleLevel || !context.OrganizationId.HasValue)
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    private static async Task<bool> CanReadOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (!context.OrganizationId.HasValue)
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    public async Task<DepartmentParLevelDto?> CreateAsync(Guid departmentToken, Guid articleToken, decimal minimumQuantity, decimal reorderQuantity, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var department = await connection.QueryFirstOrDefaultAsync<Department>(
            "sp_Department_GetByToken", new { DepartmentToken = departmentToken }, commandType: CommandType.StoredProcedure);
        if (department is null)
            throw new ApiException(ErrorCodes.DepartmentNotFound, "Department not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, department.OrganizationId))
            throw new ApiException(ErrorCodes.DepartmentParLevelForbidden, "Cannot configure a par level for a department outside your scope.", 403);

        var article = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = articleToken }, commandType: CommandType.StoredProcedure);
        if (article is null)
            throw new ApiException(ErrorCodes.ArticleNotFound, "Article not found.", 404);

        if (minimumQuantity < 0)
            throw new ApiException(ErrorCodes.DepartmentParLevelInvalidQuantity, "Minimum quantity cannot be negative.", 400);
        if (reorderQuantity <= 0)
            throw new ApiException(ErrorCodes.DepartmentParLevelInvalidQuantity, "Reorder quantity must be greater than zero.", 400);

        var existing = await connection.QueryFirstOrDefaultAsync<DepartmentParLevel>(
            "sp_DepartmentParLevel_GetByDepartmentAndArticle", new { department.DepartmentId, article.ArticleId }, commandType: CommandType.StoredProcedure);
        if (existing is not null)
            throw new ApiException(ErrorCodes.DepartmentParLevelAlreadyExists, $"A par level is already configured for '{article.Name}' at this department — edit it instead.", 400);

        var created = await connection.QueryFirstOrDefaultAsync<DepartmentParLevel>(
            "sp_DepartmentParLevel_Create",
            new
            {
                DepartmentParLevelToken = Guid.NewGuid(),
                department.DepartmentId,
                article.ArticleId,
                MinimumQuantity = minimumQuantity,
                ReorderQuantity = reorderQuantity,
                CreatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return created is null ? null : mapper.Map<DepartmentParLevelDto>(created);
    }

    public async Task<DepartmentParLevelDto?> EditAsync(Guid departmentParLevelToken, decimal minimumQuantity, decimal reorderQuantity, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<DepartmentParLevel>(
            "sp_DepartmentParLevel_GetByToken", new { DepartmentParLevelToken = departmentParLevelToken }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            throw new ApiException(ErrorCodes.DepartmentParLevelNotFound, "Department par level not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId))
            throw new ApiException(ErrorCodes.DepartmentParLevelForbidden, "Cannot edit a par level outside your scope.", 403);

        if (minimumQuantity < 0)
            throw new ApiException(ErrorCodes.DepartmentParLevelInvalidQuantity, "Minimum quantity cannot be negative.", 400);
        if (reorderQuantity <= 0)
            throw new ApiException(ErrorCodes.DepartmentParLevelInvalidQuantity, "Reorder quantity must be greater than zero.", 400);

        var updated = await connection.QueryFirstOrDefaultAsync<DepartmentParLevel>(
            "sp_DepartmentParLevel_Edit",
            new
            {
                DepartmentParLevelToken = departmentParLevelToken,
                MinimumQuantity = minimumQuantity,
                ReorderQuantity = reorderQuantity,
                LastUpdatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<DepartmentParLevelDto>(updated);
    }

    public async Task<DepartmentParLevelDto?> SetActiveAsync(Guid departmentParLevelToken, bool isActive, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<DepartmentParLevel>(
            "sp_DepartmentParLevel_GetByToken", new { DepartmentParLevelToken = departmentParLevelToken }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            throw new ApiException(ErrorCodes.DepartmentParLevelNotFound, "Department par level not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId))
            throw new ApiException(ErrorCodes.DepartmentParLevelForbidden, "Cannot change a par level outside your scope.", 403);

        var updated = await connection.QueryFirstOrDefaultAsync<DepartmentParLevel>(
            "sp_DepartmentParLevel_SetActive",
            new { DepartmentParLevelToken = departmentParLevelToken, IsActive = isActive, LastUpdatedBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<DepartmentParLevelDto>(updated);
    }

    public async Task<DepartmentParLevelDto?> GetByDepartmentAndArticleAsync(Guid departmentToken, Guid articleToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var department = await connection.QueryFirstOrDefaultAsync<Department>(
            "sp_Department_GetByToken", new { DepartmentToken = departmentToken }, commandType: CommandType.StoredProcedure);
        if (department is null || !await CanReadOrganizationAsync(connection, context, department.OrganizationId))
            return null;

        var article = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = articleToken }, commandType: CommandType.StoredProcedure);
        if (article is null)
            return null;

        var existing = await connection.QueryFirstOrDefaultAsync<DepartmentParLevel>(
            "sp_DepartmentParLevel_GetByDepartmentAndArticle", new { department.DepartmentId, article.ArticleId }, commandType: CommandType.StoredProcedure);

        return existing is null ? null : mapper.Map<DepartmentParLevelDto>(existing);
    }

    public async Task<PagedResult<SuggestedRequisitionDto>> GetSuggestedAsync(Guid? organizationToken, Guid? departmentToken, Guid? articleToken, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        int? rootOrganizationId;
        int? departmentId = null;
        int? articleId = null;

        if (departmentToken.HasValue)
        {
            var department = await connection.QueryFirstOrDefaultAsync<Department>(
                "sp_Department_GetByToken", new { DepartmentToken = departmentToken.Value }, commandType: CommandType.StoredProcedure);

            if (department is null || !await CanReadOrganizationAsync(connection, context, department.OrganizationId))
                return new PagedResult<SuggestedRequisitionDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            departmentId = department.DepartmentId;
            rootOrganizationId = null;
        }
        else if (organizationToken.HasValue)
        {
            var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                "sp_Organization_GetByToken", new { OrganizationToken = organizationToken.Value, RootOrganizationId = (int?)null }, commandType: CommandType.StoredProcedure);

            if (organization is null || !await CanReadOrganizationAsync(connection, context, organization.OrganizationId))
                return new PagedResult<SuggestedRequisitionDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            rootOrganizationId = organization.OrganizationId;
        }
        else if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            rootOrganizationId = null; // unrestricted
        }
        else if (context.OrganizationId.HasValue)
        {
            rootOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            return new PagedResult<SuggestedRequisitionDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        if (articleToken.HasValue)
        {
            var article = await connection.QueryFirstOrDefaultAsync<Article>(
                "sp_Article_GetByToken", new { ArticleToken = articleToken.Value }, commandType: CommandType.StoredProcedure);

            if (article is null)
                return new PagedResult<SuggestedRequisitionDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            articleId = article.ArticleId;
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@DepartmentId", departmentId);
        p.Add("@ArticleId", articleId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<SuggestedRequisitionRow>(
            "sp_DepartmentParLevel_GetSuggested", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<SuggestedRequisitionDto>
        {
            Items = mapper.MapList<SuggestedRequisitionDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }
}
