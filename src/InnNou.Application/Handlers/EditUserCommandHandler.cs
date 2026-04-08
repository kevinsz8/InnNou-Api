using InnNou.Application.Common;
using InnNou.Application.Persistence;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditUserCommandHandler : IRequestHandler<EditUserCommandRequest, ApiResponse<EditUserCommandResponse>>
    {
        private readonly IUserService _userService;
        private readonly AutoMapper.IMapper _mapper;
        public EditUserCommandHandler(IUserService userService, AutoMapper.IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }
        public async Task<ApiResponse<EditUserCommandResponse>> Handle(EditUserCommandRequest request, CancellationToken cancellationToken)
        {
            var userDto = _mapper.Map<UserDto>(request);
            var updatedUser = await _userService.EditUserAsync(userDto, cancellationToken);
            if (updatedUser == null)
                return ApiResponse<EditUserCommandResponse>.FailureResponse("USER_EDIT_FAILED", "User could not be updated.");
            var response = _mapper.Map<EditUserCommandResponse>(updatedUser);
            return ApiResponse<EditUserCommandResponse>.SuccessResponse(response);
        }
    }
}
