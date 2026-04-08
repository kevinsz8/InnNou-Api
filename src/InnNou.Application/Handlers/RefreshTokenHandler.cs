using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Persistence;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class RefreshTokenHandler : IRequestHandler<RefreshTokenRequest, ApiResponse<LoginResponse>>
    {
        private readonly IAuthService _authService;
        public RefreshTokenHandler(IAuthService authService)
        {
            _authService = authService;
        }
        public async Task<ApiResponse<LoginResponse>> Handle(RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var login = await _authService.RefreshTokenAsync(request.RefreshToken, CancellationToken.None);
            if (login == null)
                return ApiResponse<LoginResponse>.FailureResponse("INVALID_CREDENTIALS", "Invalid token.");
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
