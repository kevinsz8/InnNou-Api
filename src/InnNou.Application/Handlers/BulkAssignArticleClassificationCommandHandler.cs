using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class BulkAssignArticleClassificationCommandHandler(
        IArticleClassificationService articleClassificationService,
        IArticleService articleService,
        ICategoryService categoryService,
        ISubCategoryService subCategoryService,
        IRequestContext context)
        : IRequestHandler<BulkAssignArticleClassificationCommandRequest, ApiResponse<BulkAssignArticleClassificationCommandResponse>>
    {
        // Same cap this codebase enforces on every other bulk operation (bulk import, template
        // application, price-change subscriptions) — a batch resolution being one round trip
        // instead of N doesn't remove the need for an upper bound on N itself.
        private const int MaxArticleTokens = 500;

        public async Task<ApiResponse<BulkAssignArticleClassificationCommandResponse>> Handle(BulkAssignArticleClassificationCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.ArticleTokens.Count > MaxArticleTokens)
                return ApiResponse<BulkAssignArticleClassificationCommandResponse>.FailureResponse(ErrorCodes.ArticleClassificationTooManyArticleTokens, $"A single bulk classification request cannot contain more than {MaxArticleTokens} article tokens.", 400);

            var category = await categoryService.GetByTokenAsync(request.CategoryToken, context, cancellationToken);
            if (category is null)
                return ApiResponse<BulkAssignArticleClassificationCommandResponse>.FailureResponse(ErrorCodes.ArticleClassificationCategoryNotFound, "Category not found or outside your organization's scope.", 404);

            int? subCategoryId = null;
            if (request.SubCategoryToken.HasValue)
            {
                var subCategory = await subCategoryService.GetByTokenAsync(request.SubCategoryToken.Value, context, cancellationToken);
                if (subCategory is null)
                    return ApiResponse<BulkAssignArticleClassificationCommandResponse>.FailureResponse(ErrorCodes.SubCategoryNotFound, "SubCategory not found or outside your organization's scope.", 404);

                if (subCategory.CategoryId != category.CategoryId)
                    return ApiResponse<BulkAssignArticleClassificationCommandResponse>.FailureResponse(ErrorCodes.ArticleClassificationSubCategoryMismatch, "The selected SubCategory does not belong to the selected Category.", 400);

                subCategoryId = subCategory.SubCategoryId;
            }

            // Resolve every ArticleToken up front in a single batched round trip — an unresolvable
            // token becomes a per-item error in the response rather than failing the whole batch,
            // same "partial success" shape as every other bulk operation in this codebase.
            var response = new BulkAssignArticleClassificationCommandResponse { TotalCount = request.ArticleTokens.Count };

            var resolvedTokenToId = await articleService.GetIdsByTokensAsync(request.ArticleTokens, context, cancellationToken);
            var articleIdToToken = new Dictionary<int, Guid>();
            foreach (var articleToken in request.ArticleTokens)
            {
                if (!resolvedTokenToId.TryGetValue(articleToken, out var articleId))
                {
                    response.Errors.Add(new BulkAssignArticleClassificationItemError { ArticleToken = articleToken, Code = ErrorCodes.ArticleNotFound, Description = "Article not found." });
                    continue;
                }

                articleIdToToken[articleId] = articleToken;
            }

            if (articleIdToToken.Count > 0)
            {
                var result = await articleClassificationService.BulkAssignAsync(
                    [.. articleIdToToken.Keys], category.CategoryId, subCategoryId, request.OrganizationToken, context, cancellationToken);

                response.SucceededCount = result.SucceededCount;
                foreach (var error in result.Errors)
                    response.Errors.Add(new BulkAssignArticleClassificationItemError { ArticleToken = articleIdToToken[error.ArticleId], Code = error.Code, Description = error.Description });
            }

            response.FailedCount = response.Errors.Count;

            return ApiResponse<BulkAssignArticleClassificationCommandResponse>.SuccessResponse(response);
        }
    }
}
