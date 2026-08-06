using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditRequisitionLineCommandRequest : IRequest<ApiResponse<EditRequisitionLineCommandResponse>>
    {
        public Guid RequisitionLineToken { get; set; }
        public decimal QuantityRequested { get; set; }
        public Guid? UnitToken { get; set; }
        public string? Notes { get; set; }
    }
}
