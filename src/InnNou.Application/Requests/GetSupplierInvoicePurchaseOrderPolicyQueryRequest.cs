using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetSupplierInvoicePurchaseOrderPolicyQueryRequest : IRequest<ApiResponse<GetSupplierInvoicePurchaseOrderPolicyQueryResponse>>
    {
        public Guid OrganizationToken { get; set; }
    }
}
