namespace InnNou.Infrastructure.Repositories.DbEntities
{
    // sp_SupplierInvoicePurchaseOrderPolicy_GetEffective's row shape — the nearest-organization-
    // wins resolved policy, plus whether it came from @OrganizationId itself or an inherited
    // Super Asociado ancestor.
    public class SupplierInvoicePurchaseOrderPolicy
    {
        public int SupplierInvoicePurchaseOrderPolicyId { get; set; }
        public Guid SupplierInvoicePurchaseOrderPolicyToken { get; set; }
        public int EffectiveOrganizationId { get; set; }
        public Guid EffectiveOrganizationToken { get; set; }
        public string? EffectiveOrganizationName { get; set; }
        public bool AllowMultiplePurchaseOrders { get; set; }
        public bool IsInherited { get; set; }
    }
}
