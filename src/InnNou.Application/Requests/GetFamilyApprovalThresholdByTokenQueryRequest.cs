using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetFamilyApprovalThresholdByTokenQueryRequest : IRequest<ApiResponse<GetFamilyApprovalThresholdByTokenQueryResponse>>
    {
        public Guid FamilyApprovalThresholdToken { get; set; }
    }
}
