using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetSupplierInvoicePurchaseOrderPolicyQueryResponse
    {
        // Null when no policy is configured anywhere in the organization's ancestry — the
        // caller must treat that as "multiple purchase orders allowed" (today's default).
        public SupplierInvoicePurchaseOrderPolicy? Policy { get; set; }
    }
}
