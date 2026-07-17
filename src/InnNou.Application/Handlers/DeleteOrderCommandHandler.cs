using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteOrderCommandHandler(IOrderService orderService, IRequestContext context)
        : IRequestHandler<DeleteOrderCommandRequest, ApiResponse<DeleteOrderCommandResponse>>
    {
        public async Task<ApiResponse<DeleteOrderCommandResponse>> Handle(DeleteOrderCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderToken == Guid.Empty)
                return ApiResponse<DeleteOrderCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderToken is required.", 400);

            var deleted = await orderService.DeleteAsync(request.OrderToken, context, cancellationToken);
            if (!deleted)
                return ApiResponse<DeleteOrderCommandResponse>.FailureResponse(ErrorCodes.OrderNotFound, "Order not found.", 404);

            return ApiResponse<DeleteOrderCommandResponse>.SuccessResponse(new DeleteOrderCommandResponse
            {
                OrderToken = request.OrderToken,
                Success = true
            });
        }
    }
}
