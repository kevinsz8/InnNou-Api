using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class RejectOrderApprovalStepCommandRequest : IRequest<ApiResponse<RejectOrderApprovalStepCommandResponse>>
    {
        public Guid OrderApprovalStepToken { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
