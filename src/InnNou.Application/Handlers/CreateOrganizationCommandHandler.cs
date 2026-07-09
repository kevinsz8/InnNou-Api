using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommandRequest, ApiResponse<CreateOrganizationCommandResponse>>
    {
        private readonly IOrganizationService _organizationService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public CreateOrganizationCommandHandler(IOrganizationService organizationService, IMapper mapper, IRequestContext context)
        {
            _organizationService = organizationService;
            _mapper = mapper;
            _context = context;
        }

        public async Task<ApiResponse<CreateOrganizationCommandResponse>> Handle(CreateOrganizationCommandRequest request, CancellationToken cancellationToken)
        {
            var exists = await _organizationService.OrganizationExistsByNameAsync(request.Name, cancellationToken);
            if (exists)
                return ApiResponse<CreateOrganizationCommandResponse>.FailureResponse(ErrorCodes.OrganizationAlreadyExists, "An organization with this name already exists.");

            var dto = _mapper.Map<OrganizationDto>(request);
            var created = await _organizationService.CreateOrganizationAsync(dto, _context, cancellationToken);

            if (created is null)
                return ApiResponse<CreateOrganizationCommandResponse>.FailureResponse(ErrorCodes.OrganizationCreationFailed, "Organization could not be created.");

            return ApiResponse<CreateOrganizationCommandResponse>.SuccessResponse(_mapper.Map<CreateOrganizationCommandResponse>(created), 201);
        }
    }
}
