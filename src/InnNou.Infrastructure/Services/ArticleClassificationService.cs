using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class ArticleClassificationService(IDbConnectionFactory connectionFactory, IMapper mapper) : IArticleClassificationService
{
    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;

    // Same shape as CategoryService.ResolveWriteOwnerOrganizationIdAsync — a classification is
    // always anchored to a SUPER_ASSOCIATE organization, same invariant as Categories.OrganizationId.
    // SuperAdmin may target any Super Asociado org via organizationToken; a Super Asociado's own
    // Staff+ can only ever classify on behalf of their own context.OrganizationId — any
    // client-supplied organizationToken is ignored for them.
    private async Task<int> ResolveWriteOwnerOrganizationIdAsync(IDbConnection connection, IRequestContext context, Guid? organizationToken)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            if (organizationToken.HasValue)
            {
                var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                    "sp_Organization_GetByToken", new { OrganizationToken = organizationToken.Value }, commandType: CommandType.StoredProcedure);

                if (organization is null)
                    throw new ApiException(ErrorCodes.CategoryOrganizationNotFound, "The specified owning organization was not found.", 404);

                return organization.OrganizationId;
            }

            if (context.OrganizationTypeCode == OrganizationTypeCodes.SuperAssociate && context.OrganizationId.HasValue)
                return context.OrganizationId.Value;

            throw new ApiException(ErrorCodes.ArticleClassificationForbidden, "An owning Super Asociado organization is required.", 400);
        }

        if (context.OrganizationTypeCode == OrganizationTypeCodes.SuperAssociate
            && context.RoleLevel >= StaffRoleLevel
            && context.OrganizationId.HasValue)
        {
            return context.OrganizationId.Value;
        }

        throw new ApiException(ErrorCodes.ArticleClassificationForbidden, "Insufficient permissions to classify articles.", 403);
    }

    public async Task<ArticleClassificationDto> AssignAsync(int articleId, int categoryId, int? subCategoryId, Guid? organizationToken, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var organizationId = await ResolveWriteOwnerOrganizationIdAsync(connection, context, organizationToken);
        return await AssignCoreAsync(connection, articleId, categoryId, subCategoryId, organizationId, context, cancellationToken);
    }

    private async Task<ArticleClassificationDto> AssignCoreAsync(IDbConnection connection, int articleId, int categoryId, int? subCategoryId, int organizationId, IRequestContext context, CancellationToken cancellationToken)
    {
        var p = new DynamicParameters();
        p.Add("@ArticleClassificationToken", Guid.NewGuid());
        p.Add("@ArticleId", articleId);
        p.Add("@OrganizationId", organizationId);
        p.Add("@CategoryId", categoryId);
        p.Add("@SubCategoryId", subCategoryId);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        var row = await connection.QueryFirstOrDefaultAsync<ArticleClassification>(
            "sp_ArticleClassification_Assign", p, commandType: CommandType.StoredProcedure);

        if (row is null)
            throw new ApiException(ErrorCodes.ArticleClassificationCreateFailed, "Article classification could not be assigned.", 500);

        return mapper.Map<ArticleClassificationDto>(row);
    }

    public async Task<bool> UnassignAsync(int articleId, Guid? organizationToken, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var organizationId = await ResolveWriteOwnerOrganizationIdAsync(connection, context, organizationToken);

        var p = new DynamicParameters();
        p.Add("@ArticleId", articleId);
        p.Add("@OrganizationId", organizationId);

        var deletedCount = await connection.ExecuteScalarAsync<int>(
            "sp_ArticleClassification_Unassign", p, commandType: CommandType.StoredProcedure);

        return deletedCount > 0;
    }

    public async Task<BulkAssignArticleClassificationResultDto> BulkAssignAsync(List<int> articleIds, int categoryId, int? subCategoryId, Guid? organizationToken, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var organizationId = await ResolveWriteOwnerOrganizationIdAsync(connection, context, organizationToken);

        var result = new BulkAssignArticleClassificationResultDto { TotalCount = articleIds.Count };

        // Strictly sequential — same convention as every other bulk operation in this codebase
        // (bulk import, template application) — never parallelized.
        foreach (var articleId in articleIds)
        {
            try
            {
                await AssignCoreAsync(connection, articleId, categoryId, subCategoryId, organizationId, context, cancellationToken);
                result.SucceededCount++;
            }
            catch (ApiException ex)
            {
                result.Errors.Add(new BulkAssignArticleClassificationItemErrorDto { ArticleId = articleId, Code = ex.Code, Description = ex.Message });
            }
            catch (Exception)
            {
                result.Errors.Add(new BulkAssignArticleClassificationItemErrorDto { ArticleId = articleId, Code = ErrorCodes.ArticleClassificationCreateFailed, Description = "An unexpected error occurred while classifying this article." });
            }
        }

        result.FailedCount = result.Errors.Count;
        return result;
    }
}
