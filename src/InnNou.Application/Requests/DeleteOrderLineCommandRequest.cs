using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteOrderLineCommandRequest : IRequest<ApiResponse<DeleteOrderLineCommandResponse>>
    {
        public Guid OrderLineToken { get; set; }
    }
}
