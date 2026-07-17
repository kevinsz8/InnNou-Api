using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetPurchaseOrderByTokenQueryRequest : IRequest<ApiResponse<GetPurchaseOrderByTokenQueryResponse>>
    {
        public Guid PurchaseOrderToken { get; set; }
    }
}
