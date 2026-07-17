using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteOrderCommandRequest : IRequest<ApiResponse<DeleteOrderCommandResponse>>
    {
        public Guid OrderToken { get; set; }
    }
}
