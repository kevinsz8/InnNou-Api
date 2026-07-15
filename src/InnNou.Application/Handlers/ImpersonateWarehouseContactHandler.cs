using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Persistence;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ImpersonateWarehouseContactHandler : IRequestHandler<ImpersonateWarehouseContactRequest, ApiResponse<ImpersonateResponse>>
    {
        private readonly IAuthService _authService;
        private readonly IRequestContext _context;

        public ImpersonateWarehouseContactHandler(IAuthService authService, IRequestContext context)
        {
            _authService = authService;
            _context = context;
        }

        public async Task<ApiResponse<ImpersonateResponse>> Handle(ImpersonateWarehouseContactRequest request, CancellationToken cancellationToken)
        {
            if (!_context.IsAuthenticated)
            {
                return ApiResponse<ImpersonateResponse>.FailureResponse(
                    ErrorCodes.Unauthorized,
                    "User is not authenticated"
                );
            }

            var result = await _authService.ImpersonateWarehouseContactAsync(
                _context.ActorUserToken,
                request.WarehouseContactToken,
                cancellationToken
            );

            if (result == null)
            {
                return ApiResponse<ImpersonateResponse>.FailureResponse(
                    ErrorCodes.Forbidden,
                    "You are not allowed to impersonate this warehouse contact"
                );
            }

            var response = new ImpersonateResponse
            {
                Token = result.Token,
                RefreshToken = result.RefreshToken,
                UserToken = result.UserToken.ToString(),
                Email = result.Email,
                WarehouseContactName = result.WarehouseContactName,
                IsImpersonating = true
            };

            return ApiResponse<ImpersonateResponse>.SuccessResponse(response);
        }
    }
}
