using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateFamilyApprovalThresholdCommandHandler(IFamilyApprovalThresholdService service, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateFamilyApprovalThresholdCommandRequest, ApiResponse<CreateFamilyApprovalThresholdCommandResponse>>
    {
        public async Task<ApiResponse<CreateFamilyApprovalThresholdCommandResponse>> Handle(CreateFamilyApprovalThresholdCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.Level < 1 || request.ThresholdAmount <= 0)
                return ApiResponse<CreateFamilyApprovalThresholdCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Level must be >= 1 and ThresholdAmount must be greater than zero.", 400);

            var result = await service.CreateAsync(request.OrganizationToken, request.FamilyToken, request.Level, request.ThresholdAmount, request.ApproverUserToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateFamilyApprovalThresholdCommandResponse>.FailureResponse(ErrorCodes.UnhandledError, "Approval threshold could not be created.", 500);

            return ApiResponse<CreateFamilyApprovalThresholdCommandResponse>.SuccessResponse(new CreateFamilyApprovalThresholdCommandResponse
            {
                FamilyApprovalThreshold = mapper.Map<Responses.Common.FamilyApprovalThreshold>(result)
            }, 201);
        }
    }
}
