using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetConsolidatedPurchaseOrderCandidatesQueryRequest : IRequest<ApiResponse<GetConsolidatedPurchaseOrderCandidatesQueryResponse>>
    {
        public Guid SupplierToken { get; set; }
        public Guid? SuperAssociateOrganizationToken { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
    }
}
