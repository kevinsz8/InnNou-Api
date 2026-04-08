using InnNou.Application.Common;
using InnNou.Application.Persistence;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommandRequest, ApiResponse<CreateUserCommandResponse>>
    {
        private readonly IUserService _userService;
        private readonly AutoMapper.IMapper _mapper;
        public CreateUserCommandHandler(IUserService userService, AutoMapper.IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }
        public async Task<ApiResponse<CreateUserCommandResponse>> Handle(CreateUserCommandRequest request, CancellationToken cancellationToken)
        {
            var userDto = _mapper.Map<UserDto>(request);

            //validate if user email exists
            var userExists = await _userService.IsUserExists(request.Email, cancellationToken);

            if (userExists)
            {
                return ApiResponse<CreateUserCommandResponse>.FailureResponse("USER_ALREADY_EXISTS", "User already exists.");
            }


            var createdUser = await _userService.CreateUserAsync(userDto, cancellationToken);
            if (createdUser == null)
                return ApiResponse<CreateUserCommandResponse>.FailureResponse("USER_CREATION_FAILED", "User could not be created.");
            var response = _mapper.Map<CreateUserCommandResponse>(createdUser);
            return ApiResponse<CreateUserCommandResponse>.SuccessResponse(response);
        }
    }
}
