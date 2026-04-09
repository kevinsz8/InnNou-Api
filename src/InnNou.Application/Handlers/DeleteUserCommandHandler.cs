using InnNou.Application.Common;
using InnNou.Application.Persistence;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommandRequest, ApiResponse<DeleteUserCommandResponse>>
    {
        private readonly IUserService _userService;
        private readonly IRequestContext _context;
        public DeleteUserCommandHandler(IUserService userService, IRequestContext requestContext)
        {
            _userService = userService;
            _context = requestContext;
        }
        public async Task<ApiResponse<DeleteUserCommandResponse>> Handle(DeleteUserCommandRequest request, CancellationToken cancellationToken)
        {
            var success = await _userService.DeleteUserAsync(request.UserToken, _context, cancellationToken);
            var response = new DeleteUserCommandResponse { UserToken = request.UserToken, Success = success };
            if (!success)
                return ApiResponse<DeleteUserCommandResponse>.FailureResponse("USER_DELETE_FAILED", "User could not be deleted.");
            return ApiResponse<DeleteUserCommandResponse>.SuccessResponse(response);
        }
    }
}
