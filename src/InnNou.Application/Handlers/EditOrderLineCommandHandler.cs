using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditOrderLineCommandHandler(IOrderService orderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<EditOrderLineCommandRequest, ApiResponse<EditOrderLineCommandResponse>>
    {
        public async Task<ApiResponse<EditOrderLineCommandResponse>> Handle(EditOrderLineCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderLineToken == Guid.Empty)
                return ApiResponse<EditOrderLineCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderLineToken is required.", 400);

            if (request.Quantity <= 0)
                return ApiResponse<EditOrderLineCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Quantity must be greater than zero.", 400);

            var line = await orderService.EditLineAsync(request.OrderLineToken, request.Quantity, context, cancellationToken);
            if (line is null)
                return ApiResponse<EditOrderLineCommandResponse>.FailureResponse(ErrorCodes.OrderNotFound, "Order line not found.", 404);

            return ApiResponse<EditOrderLineCommandResponse>.SuccessResponse(new EditOrderLineCommandResponse
            {
                OrderLine = mapper.Map<Responses.Common.OrderLine>(line)
            });
        }
    }
}
