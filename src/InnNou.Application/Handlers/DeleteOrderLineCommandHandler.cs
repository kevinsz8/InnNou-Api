using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteOrderLineCommandHandler(IOrderService orderService, IRequestContext context)
        : IRequestHandler<DeleteOrderLineCommandRequest, ApiResponse<DeleteOrderLineCommandResponse>>
    {
        public async Task<ApiResponse<DeleteOrderLineCommandResponse>> Handle(DeleteOrderLineCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderLineToken == Guid.Empty)
                return ApiResponse<DeleteOrderLineCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderLineToken is required.", 400);

            var deleted = await orderService.DeleteLineAsync(request.OrderLineToken, context, cancellationToken);
            if (!deleted)
                return ApiResponse<DeleteOrderLineCommandResponse>.FailureResponse(ErrorCodes.OrderNotFound, "Order line not found.", 404);

            return ApiResponse<DeleteOrderLineCommandResponse>.SuccessResponse(new DeleteOrderLineCommandResponse
            {
                OrderLineToken = request.OrderLineToken,
                Success = true
            });
        }
    }
}
