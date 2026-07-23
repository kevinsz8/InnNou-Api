using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    // Deliberately does NOT inject IRequestContext — see PreviewOrderApprovalStepByEmailTokenQueryHandler.
    // OrderService throws a specific ApiException (OrderApprovalEmailTokenNotFound/Expired/
    // AlreadyUsed/StepAlreadyDecided/PriorLevelPending) for every way this can fail, which
    // ExceptionHandlingBehavior turns into the matching FailureResponse.
    public class ApproveOrderApprovalStepByEmailTokenCommandHandler(IOrderService orderService, IMapper mapper)
        : IRequestHandler<ApproveOrderApprovalStepByEmailTokenCommandRequest, ApiResponse<OrderApprovalEmailApproveResultResponse>>
    {
        public async Task<ApiResponse<OrderApprovalEmailApproveResultResponse>> Handle(ApproveOrderApprovalStepByEmailTokenCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await orderService.ApproveOrderApprovalStepByEmailTokenAsync(request.Token, cancellationToken);
            return ApiResponse<OrderApprovalEmailApproveResultResponse>.SuccessResponse(mapper.Map<OrderApprovalEmailApproveResultResponse>(result));
        }
    }
}
