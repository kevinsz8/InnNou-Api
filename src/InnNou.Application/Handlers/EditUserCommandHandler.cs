using InnNou.Application.Common;
using InnNou.Application.Persistence;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditUserCommandHandler : IRequestHandler<EditUserCommandRequest, ApiResponse<EditUserCommandResponse>>
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;
        public EditUserCommandHandler(IUserService userService, IMapper mapper, IRequestContext requestContext)
        {
            _userService = userService;
            _mapper = mapper;
            _context = requestContext;
        }
        public async Task<ApiResponse<EditUserCommandResponse>> Handle(EditUserCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.Email is not null && !UserValidation.IsValidEmail(request.Email))
                return ApiResponse<EditUserCommandResponse>.FailureResponse(ErrorCodes.UserInvalidEmail, "A valid email address is required.", 400);

            if (request.Password is not null && !UserValidation.IsStrongPassword(request.Password))
                return ApiResponse<EditUserCommandResponse>.FailureResponse(ErrorCodes.UserWeakPassword, "Password must be at least 8 characters and include an uppercase letter, lowercase letter, number and special character.", 400);

            var userDto = _mapper.Map<UserDto>(request);
            var updatedUser = await _userService.EditUserAsync(userDto, _context, cancellationToken);
            if (updatedUser == null)
                return ApiResponse<EditUserCommandResponse>.FailureResponse(ErrorCodes.UserNotFound, "User not found.", 404);
            var response = _mapper.Map<EditUserCommandResponse>(updatedUser);
            return ApiResponse<EditUserCommandResponse>.SuccessResponse(response);
        }
    }
}
