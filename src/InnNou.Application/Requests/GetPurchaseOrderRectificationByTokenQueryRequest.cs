using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetPurchaseOrderRectificationByTokenQueryRequest : IRequest<ApiResponse<GetPurchaseOrderRectificationByTokenQueryResponse>>
    {
        public Guid PurchaseOrderRectificationToken { get; set; }
    }
}
