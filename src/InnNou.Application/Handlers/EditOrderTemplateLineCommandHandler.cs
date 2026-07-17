using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditOrderTemplateLineCommandHandler(IOrderTemplateService orderTemplateService, IMapper mapper, IRequestContext context)
        : IRequestHandler<EditOrderTemplateLineCommandRequest, ApiResponse<EditOrderTemplateLineCommandResponse>>
    {
        public async Task<ApiResponse<EditOrderTemplateLineCommandResponse>> Handle(EditOrderTemplateLineCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderTemplateLineToken == Guid.Empty)
                return ApiResponse<EditOrderTemplateLineCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderTemplateLineToken is required.", 400);

            if (request.Quantity <= 0)
                return ApiResponse<EditOrderTemplateLineCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Quantity must be greater than zero.", 400);

            var line = await orderTemplateService.EditLineAsync(request.OrderTemplateLineToken, request.Quantity, context, cancellationToken);
            if (line is null)
                return ApiResponse<EditOrderTemplateLineCommandResponse>.FailureResponse(ErrorCodes.OrderTemplateLineNotFound, "Order template line not found.", 404);

            return ApiResponse<EditOrderTemplateLineCommandResponse>.SuccessResponse(new EditOrderTemplateLineCommandResponse
            {
                OrderTemplateLine = mapper.Map<Responses.Common.OrderTemplateLine>(line)
            });
        }
    }
}
