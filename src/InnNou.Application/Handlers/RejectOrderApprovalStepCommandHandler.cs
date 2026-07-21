using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class RejectOrderApprovalStepCommandHandler(IOrderService orderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<RejectOrderApprovalStepCommandRequest, ApiResponse<RejectOrderApprovalStepCommandResponse>>
    {
        public async Task<ApiResponse<RejectOrderApprovalStepCommandResponse>> Handle(RejectOrderApprovalStepCommandRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return ApiResponse<RejectOrderApprovalStepCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "A rejection reason is required.", 400);

            var result = await orderService.RejectOrderApprovalStepAsync(request.OrderApprovalStepToken, request.Reason.Trim(), context, cancellationToken);
            if (result is null)
                return ApiResponse<RejectOrderApprovalStepCommandResponse>.FailureResponse(ErrorCodes.OrderApprovalStepNotFound, "Approval step not found.", 404);

            return ApiResponse<RejectOrderApprovalStepCommandResponse>.SuccessResponse(new RejectOrderApprovalStepCommandResponse
            {
                OrderApprovalStep = mapper.Map<Responses.Common.OrderApprovalStep>(result)
            });
        }
    }
}
