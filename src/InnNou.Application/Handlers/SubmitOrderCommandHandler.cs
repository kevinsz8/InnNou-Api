using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SubmitOrderCommandHandler(IOrderService orderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SubmitOrderCommandRequest, ApiResponse<SubmitOrderCommandResponse>>
    {
        public async Task<ApiResponse<SubmitOrderCommandResponse>> Handle(SubmitOrderCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderToken == Guid.Empty)
                return ApiResponse<SubmitOrderCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderToken is required.", 400);

            var order = await orderService.SubmitAsync(request.OrderToken, context, cancellationToken);
            if (order is null)
                return ApiResponse<SubmitOrderCommandResponse>.FailureResponse(ErrorCodes.OrderNotFound, "Order not found.", 404);

            return ApiResponse<SubmitOrderCommandResponse>.SuccessResponse(new SubmitOrderCommandResponse
            {
                Order = mapper.Map<Responses.Common.Order>(order)
            });
        }
    }
}
