using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CancelInternalOrderCommandHandler(IInternalOrderService internalOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CancelInternalOrderCommandRequest, ApiResponse<CancelInternalOrderCommandResponse>>
    {
        public async Task<ApiResponse<CancelInternalOrderCommandResponse>> Handle(CancelInternalOrderCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.InternalOrderToken == Guid.Empty)
                return ApiResponse<CancelInternalOrderCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "InternalOrderToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Reason))
                return ApiResponse<CancelInternalOrderCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "A cancellation reason is required.", 400);

            var result = await internalOrderService.CancelAsync(request.InternalOrderToken, request.Reason, context, cancellationToken);
            if (result is null)
                return ApiResponse<CancelInternalOrderCommandResponse>.FailureResponse(ErrorCodes.InternalOrderNotFound, "Internal order not found.", 404);

            return ApiResponse<CancelInternalOrderCommandResponse>.SuccessResponse(new CancelInternalOrderCommandResponse
            {
                InternalOrder = mapper.Map<Responses.Common.InternalOrder>(result)
            });
        }
    }
}
