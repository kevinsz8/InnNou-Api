using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class ApproveOrderApprovalStepCommandRequest : IRequest<ApiResponse<ApproveOrderApprovalStepCommandResponse>>
    {
        public Guid OrderApprovalStepToken { get; set; }
    }
}
