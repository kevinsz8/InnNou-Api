using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetOrderByTokenQueryHandler(IOrderService orderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetOrderByTokenQueryRequest, ApiResponse<GetOrderByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetOrderByTokenQueryResponse>> Handle(GetOrderByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var order = await orderService.GetByTokenAsync(request.OrderToken, context, cancellationToken);
            if (order is null)
                return ApiResponse<GetOrderByTokenQueryResponse>.FailureResponse(ErrorCodes.OrderNotFound, "Order not found.", 404);

            return ApiResponse<GetOrderByTokenQueryResponse>.SuccessResponse(new GetOrderByTokenQueryResponse
            {
                Order = mapper.Map<Responses.Common.Order>(order)
            });
        }
    }
}
