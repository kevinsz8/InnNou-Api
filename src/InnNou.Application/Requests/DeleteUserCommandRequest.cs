using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteUserCommandRequest : IRequest<ApiResponse<DeleteUserCommandResponse>>
    {
        public Guid UserToken { get; set; }
    }
}
