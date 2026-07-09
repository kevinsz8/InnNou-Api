using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetOrganizationByTokenQueryHandler : IRequestHandler<GetOrganizationByTokenQueryRequest, ApiResponse<GetOrganizationByTokenQueryResponse>>
    {
        private readonly IOrganizationService _organizationService;
        private readonly IRequestContext _context;
        private readonly IMapper _mapper;

        public GetOrganizationByTokenQueryHandler(IOrganizationService organizationService, IRequestContext context, IMapper mapper)
        {
            _organizationService = organizationService;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<GetOrganizationByTokenQueryResponse>> Handle(GetOrganizationByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var dto = await _organizationService.GetOrganizationByTokenAsync(request.OrganizationToken, _context, cancellationToken);

            if (dto is null)
                return ApiResponse<GetOrganizationByTokenQueryResponse>.FailureResponse(ErrorCodes.OrganizationNotFound, "Organization not found or access denied.", 404);

            return ApiResponse<GetOrganizationByTokenQueryResponse>.SuccessResponse(
                new GetOrganizationByTokenQueryResponse { Organization = _mapper.Map<Organization>(dto) });
        }
    }
}
