using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetEligiblePurchaseOrdersForInvoicingQueryRequest : IRequest<ApiResponse<GetEligiblePurchaseOrdersForInvoicingQueryResponse>>
    {
        public Guid OrganizationToken { get; set; }
        public Guid SupplierToken { get; set; }
    }
}
