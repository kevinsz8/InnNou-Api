using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class AddOrderTemplateLineCommandHandler(IOrderTemplateService orderTemplateService, IMapper mapper, IRequestContext context)
        : IRequestHandler<AddOrderTemplateLineCommandRequest, ApiResponse<AddOrderTemplateLineCommandResponse>>
    {
        public async Task<ApiResponse<AddOrderTemplateLineCommandResponse>> Handle(AddOrderTemplateLineCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderTemplateToken == Guid.Empty)
                return ApiResponse<AddOrderTemplateLineCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderTemplateToken is required.", 400);

            if (request.ArticleToken == Guid.Empty)
                return ApiResponse<AddOrderTemplateLineCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ArticleToken is required.", 400);

            if (request.Quantity <= 0)
                return ApiResponse<AddOrderTemplateLineCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Quantity must be greater than zero.", 400);

            var line = await orderTemplateService.AddLineAsync(request.OrderTemplateToken, request.ArticleToken, request.Quantity, context, cancellationToken);
            if (line is null)
                return ApiResponse<AddOrderTemplateLineCommandResponse>.FailureResponse(ErrorCodes.OrderTemplateNotFound, "Order template not found.", 404);

            return ApiResponse<AddOrderTemplateLineCommandResponse>.SuccessResponse(new AddOrderTemplateLineCommandResponse
            {
                OrderTemplateLine = mapper.Map<Responses.Common.OrderTemplateLine>(line)
            }, 201);
        }
    }
}
