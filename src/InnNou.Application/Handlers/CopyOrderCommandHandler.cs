using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CopyOrderCommandHandler(IOrderService orderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CopyOrderCommandRequest, ApiResponse<CopyOrderCommandResponse>>
    {
        public async Task<ApiResponse<CopyOrderCommandResponse>> Handle(CopyOrderCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderToken == Guid.Empty)
                return ApiResponse<CopyOrderCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderToken is required.", 400);

            var result = await orderService.CopyAsync(request.OrderToken, context, cancellationToken);
            var response = mapper.Map<CopyOrderCommandResponse>(result);
            return ApiResponse<CopyOrderCommandResponse>.SuccessResponse(response, 201);
        }
    }
}
