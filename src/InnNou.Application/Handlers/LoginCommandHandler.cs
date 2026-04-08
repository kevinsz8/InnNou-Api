using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Persistence;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class LoginCommandHandler : IRequestHandler<LoginCommandRequest, ApiResponse<LoginResponse>>
    {
        private readonly IAuthService _authService;
        public LoginCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }
        public async Task<ApiResponse<LoginResponse>> Handle(LoginCommandRequest request, CancellationToken cancellationToken)
        {
            var login = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
            if (login == null)
                return ApiResponse<LoginResponse>.FailureResponse("INVALID_CREDENTIALS", "Invalid email or password.");
            var response = new LoginResponse
            {
                UserId = login.UserId,
                Email = login.Email,
                Token = login.Token,
                RefreshToken = login.RefreshToken
            };
            return ApiResponse<LoginResponse>.SuccessResponse(response);
        }
    }
}
