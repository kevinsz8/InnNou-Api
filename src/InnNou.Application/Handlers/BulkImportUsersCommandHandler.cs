using InnNou.Application.Common;
using InnNou.Application.Persistence;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class BulkImportUsersCommandHandler : IRequestHandler<BulkImportUsersCommandRequest, ApiResponse<BulkImportUsersCommandResponse>>
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public BulkImportUsersCommandHandler(IUserService userService, IMapper mapper, IRequestContext requestContext)
        {
            _userService = userService;
            _mapper = mapper;
            _context = requestContext;
        }

        public async Task<ApiResponse<BulkImportUsersCommandResponse>> Handle(BulkImportUsersCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await _userService.BulkImportUsersAsync(request.FileBytes, _context, cancellationToken);
            var response = _mapper.Map<BulkImportUsersCommandResponse>(result);
            return ApiResponse<BulkImportUsersCommandResponse>.SuccessResponse(response);
        }
    }
}
