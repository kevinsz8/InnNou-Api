using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class AddRequisitionLineCommandRequest : IRequest<ApiResponse<AddRequisitionLineCommandResponse>>
    {
        public Guid RequisitionToken { get; set; }
        public Guid ArticleToken { get; set; }
        public decimal QuantityRequested { get; set; }
        public string? Notes { get; set; }
    }
}
