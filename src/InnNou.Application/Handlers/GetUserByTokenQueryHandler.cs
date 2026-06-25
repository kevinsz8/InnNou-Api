using InnNou.Application.Common;
using InnNou.Application.Persistence;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetUserByTokenQueryHandler : IRequestHandler<GetUserByTokenQueryRequest, ApiResponse<GetUserByTokenQueryResponse>>
    {
        private readonly IUserService _userService;
        private readonly IRequestContext _context;
        private readonly IMapper _mapper;

        public GetUserByTokenQueryHandler(IUserService userService, IRequestContext context, IMapper mapper)
        {
            _userService = userService;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<GetUserByTokenQueryResponse>> Handle(GetUserByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var dto = await _userService.GetUserByTokenAsync(request.UserToken, _context, cancellationToken);

            if (dto is null)
                return ApiResponse<GetUserByTokenQueryResponse>.FailureResponse("USER_NOT_FOUND", "User not found or access denied.", 404);

            return ApiResponse<GetUserByTokenQueryResponse>.SuccessResponse(
                new GetUserByTokenQueryResponse { User = _mapper.Map<User>(dto) });
        }
    }
}
