using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CloseShortRequisitionCommandRequest : IRequest<ApiResponse<CloseShortRequisitionCommandResponse>>
    {
        public Guid RequisitionToken { get; set; }
        public string Reason { get; set; } = default!;
    }
}
