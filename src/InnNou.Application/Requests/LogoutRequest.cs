using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    // Deliberately not named "...CommandRequest" — same reasoning as RefreshTokenRequest/
    // ImpersonateRequest: a session/auth flow, not a business-entity mutation, so it's excluded
    // from IdempotencyBehavior's naming-convention check for free (see IdempotencyBehavior.cs).
    public class LogoutRequest : IRequest<ApiResponse<LogoutResponse>>
    {
        public string RefreshToken { get; set; } = null!;
    }
}
