using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateUserCommandRequest : IRequest<ApiResponse<CreateUserCommandResponse>>
    {
        public required string Email { get; set; } = default!;
        public required string Password { get; set; } = default!;
        public required string FirstName { get; set; } = default!;
        public required string LastName { get; set; } = default!;
        public string UserName { get; set; } = default!;
        //public required Guid TenantId { get; set; }
    }
}
