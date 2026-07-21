using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetActiveFamilyApprovalThresholdCommandRequest : IRequest<ApiResponse<SetActiveFamilyApprovalThresholdCommandResponse>>
    {
        public Guid FamilyApprovalThresholdToken { get; set; }
        public bool IsActive { get; set; }
    }
}
