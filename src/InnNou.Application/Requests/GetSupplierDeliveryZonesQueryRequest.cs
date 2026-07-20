using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetSupplierDeliveryZonesQueryRequest : IRequest<ApiResponse<GetSupplierDeliveryZonesQueryResponse>>
    {
        public Guid? SupplierToken { get; set; }
    }
}
