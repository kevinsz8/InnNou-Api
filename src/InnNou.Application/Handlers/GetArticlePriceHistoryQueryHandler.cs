using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetArticlePriceHistoryQueryHandler(IArticlePriceService articlePriceService, IArticleService articleService, IOrganizationService organizationService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetArticlePriceHistoryQueryRequest, ApiResponse<GetArticlePriceHistoryQueryResponse>>
    {
        public async Task<ApiResponse<GetArticlePriceHistoryQueryResponse>> Handle(GetArticlePriceHistoryQueryRequest request, CancellationToken cancellationToken)
        {
            var article = await articleService.GetByTokenAsync(request.ArticleToken, context, cancellationToken);
            if (article is null)
                return ApiResponse<GetArticlePriceHistoryQueryResponse>.FailureResponse(ErrorCodes.ArticleNotFound, "Article not found.", 404);

            int? organizationId = null;
            if (request.OrganizationToken.HasValue)
            {
                var organization = await organizationService.GetOrganizationByTokenAsync(request.OrganizationToken.Value, context, cancellationToken);
                if (organization is null)
                    return ApiResponse<GetArticlePriceHistoryQueryResponse>.FailureResponse(ErrorCodes.OrganizationNotFound, "Organization not found.", 404);
                organizationId = organization.OrganizationId;
            }

            var currencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? null : request.CurrencyCode.Trim().ToUpperInvariant();

            var result = await articlePriceService.GetHistoryAsync(request.PageNumber, request.PageSize, article.ArticleId, article.SupplierId, organizationId, currencyCode, context, cancellationToken);
            var totalPages = result.TotalPages;
            var response = new GetArticlePriceHistoryQueryResponse
            {
                ArticlePrices = mapper.MapList<Responses.Common.ArticlePrice>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1,
                NextPageNumber = request.PageNumber < totalPages ? request.PageNumber + 1 : (int?)null,
                PreviousPageNumber = request.PageNumber > 1 ? request.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetArticlePriceHistoryQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
