using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetArticleDiscountsQueryHandler(IArticleDiscountService service, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetArticleDiscountsQueryRequest, ApiResponse<GetArticleDiscountsQueryResponse>>
    {
        public async Task<ApiResponse<GetArticleDiscountsQueryResponse>> Handle(GetArticleDiscountsQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await service.GetPagedAsync(request.SupplierToken, request.PageNumber, request.PageSize, request.IncludeInactive, context, cancellationToken);
            var totalPages = result.TotalPages;
            var response = new GetArticleDiscountsQueryResponse
            {
                ArticleDiscounts = mapper.MapList<Responses.Common.ArticleDiscount>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetArticleDiscountsQueryResponse>.SuccessResponse(response);
        }
    }
}
