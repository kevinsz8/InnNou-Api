using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CancelOrderCommandHandler(IOrderService orderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CancelOrderCommandRequest, ApiResponse<CancelOrderCommandResponse>>
    {
        public async Task<ApiResponse<CancelOrderCommandResponse>> Handle(CancelOrderCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderToken == Guid.Empty)
                return ApiResponse<CancelOrderCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderToken is required.", 400);

            var order = await orderService.CancelAsync(request.OrderToken, context, cancellationToken);
            if (order is null)
                return ApiResponse<CancelOrderCommandResponse>.FailureResponse(ErrorCodes.OrderNotFound, "Order not found.", 404);

            return ApiResponse<CancelOrderCommandResponse>.SuccessResponse(new CancelOrderCommandResponse
            {
                Order = mapper.Map<Responses.Common.Order>(order)
            });
        }
    }
}
