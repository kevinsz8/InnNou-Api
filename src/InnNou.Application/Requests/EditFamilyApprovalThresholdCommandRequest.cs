using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditFamilyApprovalThresholdCommandRequest : IRequest<ApiResponse<EditFamilyApprovalThresholdCommandResponse>>
    {
        public Guid FamilyApprovalThresholdToken { get; set; }
        public decimal ThresholdAmount { get; set; }
        public Guid ApproverUserToken { get; set; }
    }
}
