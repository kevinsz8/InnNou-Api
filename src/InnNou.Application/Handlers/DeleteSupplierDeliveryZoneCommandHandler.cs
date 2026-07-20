using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteSupplierDeliveryZoneCommandHandler(ISupplierDeliveryZoneService supplierDeliveryZoneService, IRequestContext context)
        : IRequestHandler<DeleteSupplierDeliveryZoneCommandRequest, ApiResponse<DeleteSupplierDeliveryZoneCommandResponse>>
    {
        public async Task<ApiResponse<DeleteSupplierDeliveryZoneCommandResponse>> Handle(DeleteSupplierDeliveryZoneCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.DayOfWeek is < 0 or > 6)
                return ApiResponse<DeleteSupplierDeliveryZoneCommandResponse>.FailureResponse(ErrorCodes.SupplierDeliveryZoneInvalidDayOfWeek, "DayOfWeek must be between 0 (Sunday) and 6 (Saturday).", 400);

            var deleted = await supplierDeliveryZoneService.DeleteAsync(request.SupplierToken, request.ZoneToken, request.DayOfWeek, context, cancellationToken);
            var response = new DeleteSupplierDeliveryZoneCommandResponse { Deleted = deleted };
            return ApiResponse<DeleteSupplierDeliveryZoneCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
