using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetRolesHandler : IRequestHandler<GetRolesQueryRequest, ApiResponse<GetRolesQueryResponse>>
    {
        private readonly IRoleService _Roleservice;
        private readonly IRequestContext _context;
        private readonly AutoMapper.IMapper _mapper;

        public GetRolesHandler(IRoleService Roleservice, IRequestContext context, AutoMapper.IMapper mapper)
        {
            _Roleservice = Roleservice;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<GetRolesQueryResponse>> Handle(GetRolesQueryRequest request, CancellationToken cancellationToken)
        {
            var resultRoles = await _Roleservice.GetRolesAsync(request.PageNumber, request.PageSize, request.SearchField, request.SearchText, _context, cancellationToken);
            var Roles = _mapper.Map<List<Role>>(resultRoles.Items);
            var totalPages = resultRoles.TotalPages;
            var response = new GetRolesQueryResponse
            {
                Roles = Roles,
                TotalCount = resultRoles.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1,
                NextPageNumber = request.PageNumber < totalPages ? request.PageNumber + 1 : (int?)null,
                PreviousPageNumber = request.PageNumber > 1 ? request.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetRolesQueryResponse>.SuccessResponse(new GetRolesQueryResponse
            {
                Roles = Roles
            });
        }
    }
}
