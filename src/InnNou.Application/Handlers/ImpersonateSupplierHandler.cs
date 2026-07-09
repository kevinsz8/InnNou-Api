using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Persistence;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ImpersonateSupplierHandler : IRequestHandler<ImpersonateSupplierRequest, ApiResponse<ImpersonateResponse>>
    {
        private readonly IAuthService _authService;
        private readonly IRequestContext _context;

        public ImpersonateSupplierHandler(IAuthService authService, IRequestContext context)
        {
            _authService = authService;
            _context = context;
        }

        public async Task<ApiResponse<ImpersonateResponse>> Handle(ImpersonateSupplierRequest request, CancellationToken cancellationToken)
        {
            if (!_context.IsAuthenticated)
            {
                return ApiResponse<ImpersonateResponse>.FailureResponse(
                    ErrorCodes.Unauthorized,
                    "User is not authenticated"
                );
            }

            var result = await _authService.ImpersonateSupplierAsync(
                _context.ActorUserToken,
                request.SupplierToken,
                cancellationToken
            );

            if (result == null)
            {
                return ApiResponse<ImpersonateResponse>.FailureResponse(
                    ErrorCodes.Forbidden,
                    "You are not allowed to impersonate this supplier"
                );
            }

            var response = new ImpersonateResponse
            {
                Token = result.Token,
                RefreshToken = result.RefreshToken,
                UserToken = result.UserToken.ToString(),
                Email = result.Email,
                SupplierName = result.SupplierName,
                IsImpersonating = true
            };

            return ApiResponse<ImpersonateResponse>.SuccessResponse(response);
        }
    }
}
