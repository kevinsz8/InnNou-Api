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
        // 0 = "leave the role as-is" (UserService.EditUserAsync's own convention) — the frontend's
        // Edit User form always sends the currently-selected role, but this stays optional at the
        // wire level for any other caller.
        public int RoleId { get; set; }
    }
}
