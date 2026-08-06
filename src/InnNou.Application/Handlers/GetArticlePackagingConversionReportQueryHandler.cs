using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetArticlePackagingConversionReportQueryHandler(IArticleService articleService, IOrganizationService organizationService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetArticlePackagingConversionReportQueryRequest, ApiResponse<GetArticlePackagingConversionReportQueryResponse>>
    {
        public async Task<ApiResponse<GetArticlePackagingConversionReportQueryResponse>> Handle(GetArticlePackagingConversionReportQueryRequest request, CancellationToken cancellationToken)
        {
            int? organizationId = null;
            if (request.OrganizationToken.HasValue)
            {
                // Same "GetOrganizationByTokenAsync already enforces scope" reasoning as
                // GetArticlesQueryHandler's own OrganizationToken handling.
                var organization = await organizationService.GetOrganizationByTokenAsync(request.OrganizationToken.Value, context, cancellationToken);
                if (organization is null)
                    return ApiResponse<GetArticlePackagingConversionReportQueryResponse>.FailureResponse(ErrorCodes.OrganizationOutsideScope, "Organization not found or outside your scope.", 403);
                organizationId = organization.OrganizationId;
            }

            var result = await articleService.GetPackagingConversionReportAsync(request.PageNumber, request.PageSize, request.SearchText, request.IncludeInactive, organizationId, context, cancellationToken);
            var totalPages = result.TotalPages;
            var response = new GetArticlePackagingConversionReportQueryResponse
            {
                Articles = mapper.MapList<Responses.Common.ArticlePackagingConversion>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1,
                NextPageNumber = request.PageNumber < totalPages ? request.PageNumber + 1 : (int?)null,
                PreviousPageNumber = request.PageNumber > 1 ? request.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetArticlePackagingConversionReportQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
