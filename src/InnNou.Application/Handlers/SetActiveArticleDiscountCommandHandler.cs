using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetActiveArticleDiscountCommandHandler(IArticleDiscountService service, IMapper mapper, IRequestContext context)
        : IRequestHandler<SetActiveArticleDiscountCommandRequest, ApiResponse<SetActiveArticleDiscountCommandResponse>>
    {
        public async Task<ApiResponse<SetActiveArticleDiscountCommandResponse>> Handle(SetActiveArticleDiscountCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await service.SetActiveAsync(request.ArticleDiscountToken, request.IsActive, context, cancellationToken);
            if (result is null)
                return ApiResponse<SetActiveArticleDiscountCommandResponse>.FailureResponse(ErrorCodes.ArticleDiscountNotFound, "Discount not found.", 404);

            return ApiResponse<SetActiveArticleDiscountCommandResponse>.SuccessResponse(new SetActiveArticleDiscountCommandResponse
            {
                ArticleDiscount = mapper.Map<Responses.Common.ArticleDiscount>(result)
            });
        }
    }
}
