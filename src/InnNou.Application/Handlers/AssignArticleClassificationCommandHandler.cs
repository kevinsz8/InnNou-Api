using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class AssignArticleClassificationCommandHandler(
        IArticleClassificationService articleClassificationService,
        IArticleService articleService,
        ICategoryService categoryService,
        ISubCategoryService subCategoryService,
        IMapper mapper,
        IRequestContext context)
        : IRequestHandler<AssignArticleClassificationCommandRequest, ApiResponse<AssignArticleClassificationCommandResponse>>
    {
        public async Task<ApiResponse<AssignArticleClassificationCommandResponse>> Handle(AssignArticleClassificationCommandRequest request, CancellationToken cancellationToken)
        {
            var article = await articleService.GetByTokenAsync(request.ArticleToken, context, cancellationToken);
            if (article is null)
                return ApiResponse<AssignArticleClassificationCommandResponse>.FailureResponse(ErrorCodes.ArticleNotFound, "Article not found.", 404);

            // Reuses CategoryService/SubCategoryService's own visibility-scoped read as the gate —
            // same "reuse an existing scoped lookup" convention used for Orders' cross-org
            // OrganizationToken override — so a category outside the assigning org's scope 404s
            // here without ArticleClassificationService needing its own visibility logic.
            var category = await categoryService.GetByTokenAsync(request.CategoryToken, context, cancellationToken);
            if (category is null)
                return ApiResponse<AssignArticleClassificationCommandResponse>.FailureResponse(ErrorCodes.ArticleClassificationCategoryNotFound, "Category not found or outside your organization's scope.", 404);

            int? subCategoryId = null;
            if (request.SubCategoryToken.HasValue)
            {
                var subCategory = await subCategoryService.GetByTokenAsync(request.SubCategoryToken.Value, context, cancellationToken);
                if (subCategory is null)
                    return ApiResponse<AssignArticleClassificationCommandResponse>.FailureResponse(ErrorCodes.SubCategoryNotFound, "SubCategory not found or outside your organization's scope.", 404);

                if (subCategory.CategoryId != category.CategoryId)
                    return ApiResponse<AssignArticleClassificationCommandResponse>.FailureResponse(ErrorCodes.ArticleClassificationSubCategoryMismatch, "The selected SubCategory does not belong to the selected Category.", 400);

                subCategoryId = subCategory.SubCategoryId;
            }

            var result = await articleClassificationService.AssignAsync(article.ArticleId, category.CategoryId, subCategoryId, request.OrganizationToken, context, cancellationToken);

            return ApiResponse<AssignArticleClassificationCommandResponse>.SuccessResponse(
                new AssignArticleClassificationCommandResponse { ArticleClassification = mapper.Map<Responses.Common.ArticleClassification>(result) }, 201);
        }
    }
}
