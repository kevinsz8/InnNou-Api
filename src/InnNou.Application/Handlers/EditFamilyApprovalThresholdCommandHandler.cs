using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditFamilyApprovalThresholdCommandHandler(IFamilyApprovalThresholdService service, IMapper mapper, IRequestContext context)
        : IRequestHandler<EditFamilyApprovalThresholdCommandRequest, ApiResponse<EditFamilyApprovalThresholdCommandResponse>>
    {
        public async Task<ApiResponse<EditFamilyApprovalThresholdCommandResponse>> Handle(EditFamilyApprovalThresholdCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await service.EditAsync(request.FamilyApprovalThresholdToken, request.ThresholdAmount, request.ApproverUserToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<EditFamilyApprovalThresholdCommandResponse>.FailureResponse(ErrorCodes.FamilyApprovalThresholdNotFound, "Approval threshold not found.", 404);

            return ApiResponse<EditFamilyApprovalThresholdCommandResponse>.SuccessResponse(new EditFamilyApprovalThresholdCommandResponse
            {
                FamilyApprovalThreshold = mapper.Map<Responses.Common.FamilyApprovalThreshold>(result)
            });
        }
    }
}
