namespace InnNou.Application.Responses.Common
{
    public class SupplierInvoicePurchaseOrderPolicy
    {
        public Guid SupplierInvoicePurchaseOrderPolicyToken { get; set; }
        public Guid EffectiveOrganizationToken { get; set; }
        public string? EffectiveOrganizationName { get; set; }
        public bool AllowMultiplePurchaseOrders { get; set; }
        public bool IsInherited { get; set; }
    }
}
