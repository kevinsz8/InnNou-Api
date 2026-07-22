using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateWarehouseCommandHandler(IWarehouseService warehouseService, IZoneService zoneService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateWarehouseCommandRequest, ApiResponse<CreateWarehouseCommandResponse>>
    {
        public async Task<ApiResponse<CreateWarehouseCommandResponse>> Handle(CreateWarehouseCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrganizationToken == Guid.Empty)
                return ApiResponse<CreateWarehouseCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrganizationToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Name))
                return ApiResponse<CreateWarehouseCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Name is required.", 400);

            var dto = mapper.Map<WarehouseDto>(request);

            if (request.ZoneToken.HasValue)
            {
                var zone = await zoneService.GetByTokenAsync(request.ZoneToken.Value, cancellationToken);
                if (zone is null || !zone.IsActive)
                    return ApiResponse<CreateWarehouseCommandResponse>.FailureResponse(ErrorCodes.WarehouseInvalidZone, "Zone must be a recognized, active zone.", 400);
                dto.ZoneId = zone.ZoneId;
            }

            var result = await warehouseService.CreateAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateWarehouseCommandResponse>.FailureResponse(ErrorCodes.WarehouseOrganizationNotFound, "Organization not found.", 404);

            return ApiResponse<CreateWarehouseCommandResponse>.SuccessResponse(mapper.Map<CreateWarehouseCommandResponse>(result), 201);
        }
    }
}
