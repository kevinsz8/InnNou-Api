using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetRoleByTokenQueryHandler : IRequestHandler<GetRoleByTokenQueryRequest, ApiResponse<GetRoleByTokenQueryResponse>>
    {
        private readonly IRoleService _roleService;
        private readonly IRequestContext _context;
        private readonly IMapper _mapper;

        public GetRoleByTokenQueryHandler(IRoleService roleService, IRequestContext context, IMapper mapper)
        {
            _roleService = roleService;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<GetRoleByTokenQueryResponse>> Handle(GetRoleByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var dto = await _roleService.GetRoleByTokenAsync(request.RoleToken, _context, cancellationToken);

            if (dto is null)
                return ApiResponse<GetRoleByTokenQueryResponse>.FailureResponse("ROLE_NOT_FOUND", "Role not found or access denied.", 404);

            return ApiResponse<GetRoleByTokenQueryResponse>.SuccessResponse(
                new GetRoleByTokenQueryResponse { Role = _mapper.Map<Role>(dto) });
        }
    }
}
