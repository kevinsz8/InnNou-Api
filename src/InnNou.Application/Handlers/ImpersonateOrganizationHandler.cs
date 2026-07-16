using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Persistence;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ImpersonateOrganizationHandler : IRequestHandler<ImpersonateOrganizationRequest, ApiResponse<ImpersonateResponse>>
    {
        private readonly IAuthService _authService;
        private readonly IRequestContext _context;

        public ImpersonateOrganizationHandler(IAuthService authService, IRequestContext context)
        {
            _authService = authService;
            _context = context;
        }

        public async Task<ApiResponse<ImpersonateResponse>> Handle(ImpersonateOrganizationRequest request, CancellationToken cancellationToken)
        {
            if (!_context.IsAuthenticated)
            {
                return ApiResponse<ImpersonateResponse>.FailureResponse(
                    ErrorCodes.Unauthorized,
                    "User is not authenticated"
                );
            }

            var result = await _authService.ImpersonateOrganizationAsync(
                _context.ActorUserToken,
                request.OrganizationToken,
                cancellationToken
            );

            if (result == null)
            {
                return ApiResponse<ImpersonateResponse>.FailureResponse(
                    ErrorCodes.Forbidden,
                    "You are not allowed to impersonate this organization"
                );
            }

            var response = new ImpersonateResponse
            {
                Token = result.Token,
                RefreshToken = result.RefreshToken,
                UserToken = result.UserToken.ToString(),
                Email = result.Email,
                OrganizationName = result.OrganizationName,
                IsImpersonating = true
            };

            return ApiResponse<ImpersonateResponse>.SuccessResponse(response);
        }
    }
}
