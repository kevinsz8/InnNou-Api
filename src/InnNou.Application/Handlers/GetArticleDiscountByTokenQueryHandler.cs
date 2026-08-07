using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetArticleDiscountByTokenQueryHandler(IArticleDiscountService service, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetArticleDiscountByTokenQueryRequest, ApiResponse<GetArticleDiscountByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetArticleDiscountByTokenQueryResponse>> Handle(GetArticleDiscountByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await service.GetByTokenAsync(request.ArticleDiscountToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<GetArticleDiscountByTokenQueryResponse>.FailureResponse(ErrorCodes.ArticleDiscountNotFound, "Discount not found.", 404);

            return ApiResponse<GetArticleDiscountByTokenQueryResponse>.SuccessResponse(new GetArticleDiscountByTokenQueryResponse
            {
                ArticleDiscount = mapper.Map<Responses.Common.ArticleDiscount>(result)
            });
        }
    }
}
