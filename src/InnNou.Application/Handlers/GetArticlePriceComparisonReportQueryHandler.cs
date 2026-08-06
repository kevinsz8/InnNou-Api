using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetArticlePriceComparisonReportQueryHandler(
        IArticleService articleService,
        ICategoryService categoryService,
        ISubCategoryService subCategoryService,
        IOrganizationService organizationService,
        IMapper mapper,
        IRequestContext context)
        : IRequestHandler<GetArticlePriceComparisonReportQueryRequest, ApiResponse<GetArticlePriceComparisonReportQueryResponse>>
    {
        public async Task<ApiResponse<GetArticlePriceComparisonReportQueryResponse>> Handle(GetArticlePriceComparisonReportQueryRequest request, CancellationToken cancellationToken)
        {
            var category = await categoryService.GetByTokenAsync(request.CategoryToken, context, cancellationToken);
            if (category is null)
                return ApiResponse<GetArticlePriceComparisonReportQueryResponse>.FailureResponse(ErrorCodes.CategoryNotFound, "Category not found.", 404);

            int? subCategoryId = null;
            if (request.SubCategoryToken.HasValue)
            {
                var subCategory = await subCategoryService.GetByTokenAsync(request.SubCategoryToken.Value, context, cancellationToken);
                if (subCategory is null)
                    return ApiResponse<GetArticlePriceComparisonReportQueryResponse>.FailureResponse(ErrorCodes.SubCategoryNotFound, "Sub-category not found.", 404);
                subCategoryId = subCategory.SubCategoryId;
            }

            int? organizationId = null;
            if (request.OrganizationToken.HasValue)
            {
                // Same "GetOrganizationByTokenAsync already enforces scope" reasoning as
                // GetArticlePackagingConversionReportQueryHandler's own OrganizationToken handling.
                var organization = await organizationService.GetOrganizationByTokenAsync(request.OrganizationToken.Value, context, cancellationToken);
                if (organization is null)
                    return ApiResponse<GetArticlePriceComparisonReportQueryResponse>.FailureResponse(ErrorCodes.OrganizationOutsideScope, "Organization not found or outside your scope.", 403);
                organizationId = organization.OrganizationId;
            }

            var result = await articleService.GetPriceComparisonReportAsync(category.CategoryId, subCategoryId, organizationId, context, cancellationToken);
            var response = new GetArticlePriceComparisonReportQueryResponse
            {
                Articles = mapper.MapList<Responses.Common.ArticlePriceComparison>(result)
            };
            return ApiResponse<GetArticlePriceComparisonReportQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
