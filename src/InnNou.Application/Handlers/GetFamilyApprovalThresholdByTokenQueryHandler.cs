using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetFamilyApprovalThresholdByTokenQueryHandler(IFamilyApprovalThresholdService service, IMapper mapper)
        : IRequestHandler<GetFamilyApprovalThresholdByTokenQueryRequest, ApiResponse<GetFamilyApprovalThresholdByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetFamilyApprovalThresholdByTokenQueryResponse>> Handle(GetFamilyApprovalThresholdByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await service.GetByTokenAsync(request.FamilyApprovalThresholdToken, cancellationToken);
            if (result is null)
                return ApiResponse<GetFamilyApprovalThresholdByTokenQueryResponse>.FailureResponse(ErrorCodes.FamilyApprovalThresholdNotFound, "Approval threshold not found.", 404);

            return ApiResponse<GetFamilyApprovalThresholdByTokenQueryResponse>.SuccessResponse(new GetFamilyApprovalThresholdByTokenQueryResponse
            {
                FamilyApprovalThreshold = mapper.Map<Responses.Common.FamilyApprovalThreshold>(result)
            });
        }
    }
}
