using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditOrganizationCommandHandler : IRequestHandler<EditOrganizationCommandRequest, ApiResponse<EditOrganizationCommandResponse>>
    {
        private readonly IOrganizationService _organizationService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public EditOrganizationCommandHandler(IOrganizationService organizationService, IMapper mapper, IRequestContext context)
        {
            _organizationService = organizationService;
            _mapper = mapper;
            _context = context;
        }

        public async Task<ApiResponse<EditOrganizationCommandResponse>> Handle(EditOrganizationCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = _mapper.Map<OrganizationDto>(request);
            var updated = await _organizationService.EditOrganizationAsync(dto, _context, cancellationToken);

            if (updated is null)
                return ApiResponse<EditOrganizationCommandResponse>.FailureResponse("ORGANIZATION_EDIT_FAILED", "Organization could not be updated.");

            return ApiResponse<EditOrganizationCommandResponse>.SuccessResponse(_mapper.Map<EditOrganizationCommandResponse>(updated));
        }
    }
}
