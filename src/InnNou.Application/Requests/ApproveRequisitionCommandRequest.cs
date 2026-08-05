using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class ApproveRequisitionCommandRequest : IRequest<ApiResponse<ApproveRequisitionCommandResponse>>
    {
        public Guid RequisitionToken { get; set; }
    }
}
