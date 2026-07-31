using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class UpsertSupplierInvoicePurchaseOrderPolicyCommandRequest : IRequest<ApiResponse<UpsertSupplierInvoicePurchaseOrderPolicyCommandResponse>>
    {
        public Guid OrganizationToken { get; set; }
        public bool AllowMultiplePurchaseOrders { get; set; }
    }
}
