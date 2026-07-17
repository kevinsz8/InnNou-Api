using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditOrderLineCommandRequest : IRequest<ApiResponse<EditOrderLineCommandResponse>>
    {
        public Guid OrderLineToken { get; set; }
        public decimal Quantity { get; set; }
    }
}
