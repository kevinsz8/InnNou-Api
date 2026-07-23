using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetConsolidatedPurchaseOrderByTokenQueryRequest : IRequest<ApiResponse<GetConsolidatedPurchaseOrderByTokenQueryResponse>>
    {
        public Guid ConsolidatedPurchaseOrderToken { get; set; }
    }
}
