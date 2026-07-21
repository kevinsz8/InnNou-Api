using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetActiveFamilyApprovalThresholdCommandHandler(IFamilyApprovalThresholdService service, IMapper mapper, IRequestContext context)
        : IRequestHandler<SetActiveFamilyApprovalThresholdCommandRequest, ApiResponse<SetActiveFamilyApprovalThresholdCommandResponse>>
    {
        public async Task<ApiResponse<SetActiveFamilyApprovalThresholdCommandResponse>> Handle(SetActiveFamilyApprovalThresholdCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await service.SetActiveAsync(request.FamilyApprovalThresholdToken, request.IsActive, context, cancellationToken);
            if (result is null)
                return ApiResponse<SetActiveFamilyApprovalThresholdCommandResponse>.FailureResponse(ErrorCodes.FamilyApprovalThresholdNotFound, "Approval threshold not found.", 404);

            return ApiResponse<SetActiveFamilyApprovalThresholdCommandResponse>.SuccessResponse(new SetActiveFamilyApprovalThresholdCommandResponse
            {
                FamilyApprovalThreshold = mapper.Map<Responses.Common.FamilyApprovalThreshold>(result)
            });
        }
    }
}
