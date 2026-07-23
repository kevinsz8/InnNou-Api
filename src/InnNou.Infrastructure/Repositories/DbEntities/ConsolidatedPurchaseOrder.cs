namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class ConsolidatedPurchaseOrder
    {
        public int ConsolidatedPurchaseOrderId { get; set; }
        public Guid ConsolidatedPurchaseOrderToken { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int SuperAssociateOrganizationId { get; set; }
        public Guid SuperAssociateOrganizationToken { get; set; }
        public string? SuperAssociateOrganizationName { get; set; }
        public string? Title { get; set; }
        public string? Notes { get; set; }
        public DateTime DateRangeFrom { get; set; }
        public DateTime DateRangeTo { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        // Only populated by sp_ConsolidatedPurchaseOrder_GetPaged (a cheap CROSS APPLY COUNT);
        // GetByToken leaves this at 0 and the service overwrites it from the real hydrated
        // Members.Count instead — same convention as PurchaseOrder.LineCount.
        public int MemberCount { get; set; }
    }
}
