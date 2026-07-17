using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class AddOrderLineCommandHandler(IOrderService orderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<AddOrderLineCommandRequest, ApiResponse<AddOrderLineCommandResponse>>
    {
        public async Task<ApiResponse<AddOrderLineCommandResponse>> Handle(AddOrderLineCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderToken == Guid.Empty)
                return ApiResponse<AddOrderLineCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderToken is required.", 400);

            if (request.ArticleToken == Guid.Empty)
                return ApiResponse<AddOrderLineCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ArticleToken is required.", 400);

            if (request.Quantity <= 0)
                return ApiResponse<AddOrderLineCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Quantity must be greater than zero.", 400);

            var line = await orderService.AddLineAsync(request.OrderToken, request.ArticleToken, request.Quantity, context, cancellationToken);
            if (line is null)
                return ApiResponse<AddOrderLineCommandResponse>.FailureResponse(ErrorCodes.OrderNotFound, "Order not found.", 404);

            return ApiResponse<AddOrderLineCommandResponse>.SuccessResponse(new AddOrderLineCommandResponse
            {
                OrderLine = mapper.Map<Responses.Common.OrderLine>(line)
            }, 201);
        }
    }
}
