using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateSupplierDeliveryZoneCommandHandler(ISupplierDeliveryZoneService supplierDeliveryZoneService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateSupplierDeliveryZoneCommandRequest, ApiResponse<CreateSupplierDeliveryZoneCommandResponse>>
    {
        public async Task<ApiResponse<CreateSupplierDeliveryZoneCommandResponse>> Handle(CreateSupplierDeliveryZoneCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.DayOfWeek is < 0 or > 6)
                return ApiResponse<CreateSupplierDeliveryZoneCommandResponse>.FailureResponse(ErrorCodes.SupplierDeliveryZoneInvalidDayOfWeek, "DayOfWeek must be between 0 (Sunday) and 6 (Saturday).", 400);

            var result = await supplierDeliveryZoneService.CreateAsync(request.SupplierToken, request.ZoneToken, request.DayOfWeek, context, cancellationToken);

            var response = new CreateSupplierDeliveryZoneCommandResponse { SupplierDeliveryZone = mapper.Map<Responses.Common.SupplierDeliveryZone>(result) };
            return ApiResponse<CreateSupplierDeliveryZoneCommandResponse>.SuccessResponse(response, 201);
        }
    }
}
