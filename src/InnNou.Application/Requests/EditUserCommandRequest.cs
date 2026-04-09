using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditUserCommandRequest : IRequest<ApiResponse<EditUserCommandResponse>>
    {
        public int UserId { get; set; }
        public Guid UserToken {  get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Password { get; set; }
        public string? UserName { get; set; }
    }
}
