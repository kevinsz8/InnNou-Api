using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetConsolidatedPurchaseOrdersQueryRequest : IRequest<ApiResponse<GetConsolidatedPurchaseOrdersQueryResponse>>
    {
        public Guid? SuperAssociateOrganizationToken { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
