using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSupplierDeliveryZonesQueryHandler(ISupplierDeliveryZoneService supplierDeliveryZoneService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetSupplierDeliveryZonesQueryRequest, ApiResponse<GetSupplierDeliveryZonesQueryResponse>>
    {
        public async Task<ApiResponse<GetSupplierDeliveryZonesQueryResponse>> Handle(GetSupplierDeliveryZonesQueryRequest request, CancellationToken cancellationToken)
        {
            var items = await supplierDeliveryZoneService.GetBySupplierAsync(request.SupplierToken, context, cancellationToken);
            var response = new GetSupplierDeliveryZonesQueryResponse { Items = mapper.MapList<Responses.Common.SupplierDeliveryZone>(items) };
            return ApiResponse<GetSupplierDeliveryZonesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
