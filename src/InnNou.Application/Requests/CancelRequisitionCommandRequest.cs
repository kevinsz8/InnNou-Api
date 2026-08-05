using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CancelRequisitionCommandRequest : IRequest<ApiResponse<CancelRequisitionCommandResponse>>
    {
        public Guid RequisitionToken { get; set; }
        public string? Reason { get; set; }
    }
}
