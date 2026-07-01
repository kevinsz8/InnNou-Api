using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetOrganizationsHandler : IRequestHandler<GetOrganizationsQueryRequest, ApiResponse<GetOrganizationsQueryResponse>>
    {
        private readonly IOrganizationService _organizationService;
        private readonly IRequestContext _context;
        private readonly IMapper _mapper;

        public GetOrganizationsHandler(IOrganizationService organizationService, IRequestContext context, IMapper mapper)
        {
            _organizationService = organizationService;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<GetOrganizationsQueryResponse>> Handle(GetOrganizationsQueryRequest request, CancellationToken cancellationToken)
        {
            var resultOrganizations = await _organizationService.GetOrganizationsAsync(request.PageNumber, request.PageSize, request.SearchField, request.SearchText, request.IncludeInactive, _context, cancellationToken);
            var organizations = _mapper.MapList<Organization>(resultOrganizations.Items);
            var totalPages = resultOrganizations.TotalPages;
            var response = new GetOrganizationsQueryResponse
            {
                Organizations = organizations,
                TotalCount = resultOrganizations.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1,
                NextPageNumber = request.PageNumber < totalPages ? request.PageNumber + 1 : (int?)null,
                PreviousPageNumber = request.PageNumber > 1 ? request.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetOrganizationsQueryResponse>.SuccessResponse(response);
        }
    }
}
