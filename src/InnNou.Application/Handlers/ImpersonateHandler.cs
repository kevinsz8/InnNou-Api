using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Persistence;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ImpersonateHandler : IRequestHandler<ImpersonateRequest, ApiResponse<ImpersonateResponse>>
    {
        private readonly IAuthService _authService;
        private readonly IRequestContext _context;

        public ImpersonateHandler(IAuthService authService, IRequestContext context)
        {
            _authService = authService;
            _context = context;
        }

        public async Task<ApiResponse<ImpersonateResponse>> Handle(ImpersonateRequest request, CancellationToken cancellationToken)
        {
            if (!_context.IsAuthenticated)
            {
                return ApiResponse<ImpersonateResponse>.FailureResponse(
                    "UNAUTHORIZED",
                    "User is not authenticated"
                );
            }

            var result = await _authService.ImpersonateAsync(
                _context.ActorUserToken,
                request.TargetUserToken,
                cancellationToken
            );

            if (result == null)
            {
                return ApiResponse<ImpersonateResponse>.FailureResponse(
                    "FORBIDDEN",
                    "You are not allowed to impersonate this user"
                );
            }

            var response = new ImpersonateResponse
            {
                Token = result.Token,
                RefreshToken = result.RefreshToken,
                UserToken = result.UserToken.ToString(),
                Email = result.Email,
                IsImpersonating = true
            };

            return ApiResponse<ImpersonateResponse>.SuccessResponse(response);
        }
    }
}
