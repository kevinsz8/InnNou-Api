using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Persistence;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InnNou.Application.Handlers
{
    public class StopImpersonateCommandHandler
        : IRequestHandler<StopImpersonateCommandRequest, ApiResponse<StopImpersonateCommandResponse>>
    {
        private readonly IAuthService _authService;
        private readonly IRequestContext _currentUserContext;

        public StopImpersonateCommandHandler(
            IAuthService authService,
            IRequestContext currentUserContext)
        {
            _authService = authService;
            _currentUserContext = currentUserContext;
        }

        public async Task<ApiResponse<StopImpersonateCommandResponse>> Handle(
            StopImpersonateCommandRequest request,
            CancellationToken cancellationToken)
        {
            if (_currentUserContext.ActorUserToken == Guid.Empty)
            {
                return ApiResponse<StopImpersonateCommandResponse>.FailureResponse("Authentication_Failed","Authenticated user token was not found.");
            }

            if (!_currentUserContext.IsImpersonating)
            {
                return ApiResponse<StopImpersonateCommandResponse>.FailureResponse("Authentication_Failed", "Current session is not impersonating any user.");
            }

            var result = await _authService.StopImpersonationAsync(
                _currentUserContext.ActorUserToken,
                cancellationToken);

            if (result == null)
            {
                return ApiResponse<StopImpersonateCommandResponse>.FailureResponse("Authentication_Failed", "Unable to stop impersonation.");
            }

            return ApiResponse<StopImpersonateCommandResponse>.SuccessResponse(
                new StopImpersonateCommandResponse
                {
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    Email = result.Email,
                    UserId = result.UserId,
                    UserToken = result.UserToken
                });
        }
    }
}
