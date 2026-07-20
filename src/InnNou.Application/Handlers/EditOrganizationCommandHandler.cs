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
        private readonly ICurrencyService _currencyService;
        private readonly IZoneService _zoneService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public EditOrganizationCommandHandler(IOrganizationService organizationService, ICurrencyService currencyService, IZoneService zoneService, IMapper mapper, IRequestContext context)
        {
            _organizationService = organizationService;
            _currencyService = currencyService;
            _zoneService = zoneService;
            _mapper = mapper;
            _context = context;
        }

        public async Task<ApiResponse<EditOrganizationCommandResponse>> Handle(EditOrganizationCommandRequest request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
            {
                request.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
                if (!await _currencyService.ExistsActiveByCodeAsync(request.CurrencyCode, cancellationToken))
                    return ApiResponse<EditOrganizationCommandResponse>.FailureResponse(ErrorCodes.OrganizationInvalidCurrency, "Currency must be a recognized, active currency.", 400);
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
                    return ApiResponse<EditOrganizationCommandResponse>.FailureResponse(ErrorCodes.OrganizationInvalidZone, "Zone must be a recognized, active zone.", 400);
                dto.ZoneId = zone.ZoneId;
            }
            var updated = await _organizationService.EditOrganizationAsync(dto, _context, cancellationToken);

            if (updated is null)
                return ApiResponse<EditOrganizationCommandResponse>.FailureResponse(ErrorCodes.OrganizationNotFound, "Organization not found.", 404);

            return ApiResponse<EditOrganizationCommandResponse>.SuccessResponse(_mapper.Map<EditOrganizationCommandResponse>(updated));
        }
    }
}
