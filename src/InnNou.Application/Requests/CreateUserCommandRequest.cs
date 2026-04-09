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
        public int RoleId { get; set; }
        public int? HotelId { get; set; }
        //public required Guid TenantId { get; set; }
    }
}
