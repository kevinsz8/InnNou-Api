using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class ApproveOrderApprovalStepByEmailTokenCommandRequest : IRequest<ApiResponse<OrderApprovalEmailApproveResultResponse>>
    {
        public Guid Token { get; set; }
    }
}
