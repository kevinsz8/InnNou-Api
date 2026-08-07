using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateArticleDiscountCommandHandler(IArticleDiscountService service, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateArticleDiscountCommandRequest, ApiResponse<CreateArticleDiscountCommandResponse>>
    {
        public async Task<ApiResponse<CreateArticleDiscountCommandResponse>> Handle(CreateArticleDiscountCommandRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.DiscountTypeCode) || request.DiscountValue <= 0 || request.EffectiveFrom == default)
                return ApiResponse<CreateArticleDiscountCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "DiscountTypeCode, a positive DiscountValue, and EffectiveFrom are required.", 400);

            var result = await service.CreateAsync(
                request.SupplierToken, request.ArticleToken, request.SubFamilyToken, request.FamilyToken,
                request.DiscountTypeCode, request.DiscountValue, request.CurrencyCode,
                request.EffectiveFrom, request.EffectiveUntil, request.Description,
                context, cancellationToken);

            if (result is null)
                return ApiResponse<CreateArticleDiscountCommandResponse>.FailureResponse(ErrorCodes.UnhandledError, "Discount could not be created.", 500);

            return ApiResponse<CreateArticleDiscountCommandResponse>.SuccessResponse(new CreateArticleDiscountCommandResponse
            {
                ArticleDiscount = mapper.Map<Responses.Common.ArticleDiscount>(result)
            }, 201);
        }
    }
}
