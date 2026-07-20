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
        private readonly ICurrencyService _currencyService;
        private readonly IZoneService _zoneService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public CreateOrganizationCommandHandler(IOrganizationService organizationService, ICurrencyService currencyService, IZoneService zoneService, IMapper mapper, IRequestContext context)
        {
            _organizationService = organizationService;
            _currencyService = currencyService;
            _zoneService = zoneService;
            _mapper = mapper;
            _context = context;
        }

        public async Task<ApiResponse<CreateOrganizationCommandResponse>> Handle(CreateOrganizationCommandRequest request, CancellationToken cancellationToken)
        {
            var exists = await _organizationService.OrganizationExistsByNameAsync(request.Name, cancellationToken);
            if (exists)
                return ApiResponse<CreateOrganizationCommandResponse>.FailureResponse(ErrorCodes.OrganizationAlreadyExists, "An organization with this name already exists.");

            if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
            {
                request.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
                if (!await _currencyService.ExistsActiveByCodeAsync(request.CurrencyCode, cancellationToken))
                    return ApiResponse<CreateOrganizationCommandResponse>.FailureResponse(ErrorCodes.OrganizationInvalidCurrency, "Currency must be a recognized, active currency.", 400);
            }
            else
            {
                request.CurrencyCode = null;
            }

            var dto = _mapper.Map<OrganizationDto>(request);

            if (request.ZoneToken.HasValue)
            {
                var zone = await _zoneService.GetByTokenAsync(request.ZoneToken.Value, cancellationToken);
                if (zone is null || !zone.IsActive)
                    return ApiResponse<CreateOrganizationCommandResponse>.FailureResponse(ErrorCodes.OrganizationInvalidZone, "Zone must be a recognized, active zone.", 400);
                dto.ZoneId = zone.ZoneId;
            }

            var created = await _organizationService.CreateOrganizationAsync(dto, _context, cancellationToken);

            if (created is null)
                return ApiResponse<CreateOrganizationCommandResponse>.FailureResponse(ErrorCodes.OrganizationCreationFailed, "Organization could not be created.");

            return ApiResponse<CreateOrganizationCommandResponse>.SuccessResponse(_mapper.Map<CreateOrganizationCommandResponse>(created), 201);
        }
    }
}
