using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateFamilyApprovalThresholdCommandRequest : IRequest<ApiResponse<CreateFamilyApprovalThresholdCommandResponse>>
    {
        public Guid OrganizationToken { get; set; }
        public Guid FamilyToken { get; set; }
        public int Level { get; set; }
        public decimal ThresholdAmount { get; set; }
        public Guid ApproverUserToken { get; set; }
    }
}
