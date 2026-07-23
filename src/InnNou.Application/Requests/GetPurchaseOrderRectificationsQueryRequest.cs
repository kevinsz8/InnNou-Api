using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetPurchaseOrderRectificationsQueryRequest : IRequest<ApiResponse<GetPurchaseOrderRectificationsQueryResponse>>
    {
        public Guid PurchaseOrderToken { get; set; }
    }
}
