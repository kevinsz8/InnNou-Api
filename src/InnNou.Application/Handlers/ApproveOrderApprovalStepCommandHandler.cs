using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ApproveOrderApprovalStepCommandHandler(IOrderService orderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<ApproveOrderApprovalStepCommandRequest, ApiResponse<ApproveOrderApprovalStepCommandResponse>>
    {
        public async Task<ApiResponse<ApproveOrderApprovalStepCommandResponse>> Handle(ApproveOrderApprovalStepCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await orderService.ApproveOrderApprovalStepAsync(request.OrderApprovalStepToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<ApproveOrderApprovalStepCommandResponse>.FailureResponse(ErrorCodes.OrderApprovalStepNotFound, "Approval step not found.", 404);

            return ApiResponse<ApproveOrderApprovalStepCommandResponse>.SuccessResponse(new ApproveOrderApprovalStepCommandResponse
            {
                OrderApprovalStep = mapper.Map<Responses.Common.OrderApprovalStep>(result)
            });
        }
    }
}
