using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    // Deliberately does NOT inject IRequestContext — this is the first read in the codebase
    // reachable with no session at all (an anonymous single-use email link). The token itself
    // is the entire authorization; OrderService throws ApiException(OrderApprovalEmailTokenNotFound)
    // for a token that was never issued, which ExceptionHandlingBehavior turns into the correct
    // 404 FailureResponse. Every other terminal state (Expired/AlreadyUsed/AlreadyDecided) is a
    // normal 200 result with Status set — not an error.
    public class PreviewOrderApprovalStepByEmailTokenQueryHandler(IOrderService orderService, IMapper mapper)
        : IRequestHandler<PreviewOrderApprovalStepByEmailTokenQueryRequest, ApiResponse<OrderApprovalEmailPreviewResponse>>
    {
        public async Task<ApiResponse<OrderApprovalEmailPreviewResponse>> Handle(PreviewOrderApprovalStepByEmailTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await orderService.GetApprovalStepPreviewByEmailTokenAsync(request.Token, cancellationToken);
            return ApiResponse<OrderApprovalEmailPreviewResponse>.SuccessResponse(mapper.Map<OrderApprovalEmailPreviewResponse>(result));
        }
    }
}
