using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateOrderCommandHandler(IOrderService orderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateOrderCommandRequest, ApiResponse<CreateOrderCommandResponse>>
    {
        public async Task<ApiResponse<CreateOrderCommandResponse>> Handle(CreateOrderCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.WarehouseToken == Guid.Empty)
                return ApiResponse<CreateOrderCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "WarehouseToken is required.", 400);

            var order = await orderService.CreateAsync(request.WarehouseToken, request.Notes, context, cancellationToken);
            if (order is null)
                return ApiResponse<CreateOrderCommandResponse>.FailureResponse(ErrorCodes.OrderWarehouseNotFound, "Warehouse not found.", 404);

            return ApiResponse<CreateOrderCommandResponse>.SuccessResponse(new CreateOrderCommandResponse
            {
                Order = mapper.Map<Responses.Common.Order>(order)
            }, 201);
        }
    }
}
