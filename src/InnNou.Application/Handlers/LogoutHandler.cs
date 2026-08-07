using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Persistence;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class LogoutHandler : IRequestHandler<LogoutRequest, ApiResponse<LogoutResponse>>
    {
        private readonly IAuthService _authService;
        public LogoutHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<ApiResponse<LogoutResponse>> Handle(LogoutRequest request, CancellationToken cancellationToken)
        {
            await _authService.LogoutAsync(request.RefreshToken, CancellationToken.None);
            return ApiResponse<LogoutResponse>.SuccessResponse(new LogoutResponse { LoggedOut = true });
        }
    }
}
