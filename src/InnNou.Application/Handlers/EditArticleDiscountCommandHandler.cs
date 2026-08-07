using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditArticleDiscountCommandHandler(IArticleDiscountService service, IMapper mapper, IRequestContext context)
        : IRequestHandler<EditArticleDiscountCommandRequest, ApiResponse<EditArticleDiscountCommandResponse>>
    {
        public async Task<ApiResponse<EditArticleDiscountCommandResponse>> Handle(EditArticleDiscountCommandRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.DiscountTypeCode) || request.DiscountValue <= 0 || request.EffectiveFrom == default)
                return ApiResponse<EditArticleDiscountCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "DiscountTypeCode, a positive DiscountValue, and EffectiveFrom are required.", 400);

            var result = await service.EditAsync(
                request.ArticleDiscountToken, request.DiscountTypeCode, request.DiscountValue, request.CurrencyCode,
                request.EffectiveFrom, request.EffectiveUntil, request.Description,
                context, cancellationToken);

            if (result is null)
                return ApiResponse<EditArticleDiscountCommandResponse>.FailureResponse(ErrorCodes.ArticleDiscountNotFound, "Discount not found.", 404);

            return ApiResponse<EditArticleDiscountCommandResponse>.SuccessResponse(new EditArticleDiscountCommandResponse
            {
                ArticleDiscount = mapper.Map<Responses.Common.ArticleDiscount>(result)
            });
        }
    }
}
