using InnNou.Application.Common;
using InnNou.Application.Persistence;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommandRequest, ApiResponse<CreateUserCommandResponse>>
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;
        public CreateUserCommandHandler(IUserService userService, IMapper mapper, IRequestContext requestContext)
        {
            _userService = userService;
            _mapper = mapper;
            _context = requestContext;
        }
        public async Task<ApiResponse<CreateUserCommandResponse>> Handle(CreateUserCommandRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.UserName))
                return ApiResponse<CreateUserCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "FirstName, LastName and UserName are required.", 400);

            if (!UserValidation.IsValidEmail(request.Email))
                return ApiResponse<CreateUserCommandResponse>.FailureResponse(ErrorCodes.UserInvalidEmail, "A valid email address is required.", 400);

            if (!UserValidation.IsStrongPassword(request.Password))
                return ApiResponse<CreateUserCommandResponse>.FailureResponse(ErrorCodes.UserWeakPassword, "Password must be at least 8 characters and include an uppercase letter, lowercase letter, number and special character.", 400);

            var userDto = _mapper.Map<UserDto>(request);

            var userExists = await _userService.IsUserExists(request.Email, cancellationToken);

            if (userExists)
            {
                return ApiResponse<CreateUserCommandResponse>.FailureResponse(ErrorCodes.UserAlreadyExists, "User already exists.");
            }

            var createdUser = await _userService.CreateUserAsync(userDto, _context, cancellationToken);
            if (createdUser == null)
                return ApiResponse<CreateUserCommandResponse>.FailureResponse(ErrorCodes.UserCreationFailed, "User could not be created.");
            var response = _mapper.Map<CreateUserCommandResponse>(createdUser);
            return ApiResponse<CreateUserCommandResponse>.SuccessResponse(response);
        }
    }
}
