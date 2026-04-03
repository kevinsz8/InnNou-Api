using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class LoginCommandRequest : IRequest<ApiResponse<LoginResponse>>
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
