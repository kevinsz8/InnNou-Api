using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class BulkImportOrganizationsCommandHandler : IRequestHandler<BulkImportOrganizationsCommandRequest, ApiResponse<BulkImportOrganizationsCommandResponse>>
    {
        private readonly IOrganizationService _organizationService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public BulkImportOrganizationsCommandHandler(IOrganizationService organizationService, IMapper mapper, IRequestContext requestContext)
        {
            _organizationService = organizationService;
            _mapper = mapper;
            _context = requestContext;
        }

        public async Task<ApiResponse<BulkImportOrganizationsCommandResponse>> Handle(BulkImportOrganizationsCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await _organizationService.BulkImportOrganizationsAsync(request.FileBytes, _context, cancellationToken);
            var response = _mapper.Map<BulkImportOrganizationsCommandResponse>(result);
            return ApiResponse<BulkImportOrganizationsCommandResponse>.SuccessResponse(response);
        }
    }
}
