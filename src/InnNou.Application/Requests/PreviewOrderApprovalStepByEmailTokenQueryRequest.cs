using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class PreviewOrderApprovalStepByEmailTokenQueryRequest : IRequest<ApiResponse<OrderApprovalEmailPreviewResponse>>
    {
        public Guid Token { get; set; }
    }
}
