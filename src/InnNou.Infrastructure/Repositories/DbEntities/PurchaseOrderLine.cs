namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class PurchaseOrderLine
    {
        public int PurchaseOrderLineId { get; set; }
        public Guid PurchaseOrderLineToken { get; set; }
        public int PurchaseOrderId { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public int OrderLineId { get; set; }
        public Guid OrderLineToken { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }

        public decimal Quantity { get; set; }

        public int PurchaseUnitId { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public decimal PurchaseQuantity { get; set; }
        public int ContentUnitId { get; set; }
        public string? ContentUnitCode { get; set; }
        public decimal? ContentQuantity { get; set; }

        public decimal UnitPrice { get; set; }
        public string CurrencyCode { get; set; } = default!;

        // Copied verbatim from the source OrderLine at Submit split time — see OrderLine.cs.
        public int? CategoryId { get; set; }
        public string? CategoryCode { get; set; }
        public int? SubCategoryId { get; set; }
        public string? SubCategoryCode { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }

        // Only populated by sp_PurchaseOrderLine_GetEffective — this row's ArticleId's own
        // FamilyId, resolved for PurchaseOrderService's rectification approval-threshold
        // recompute. Internal-use only, never mapped onto PurchaseOrderLineDto (same convention
        // as PurchaseOrder.SupplierEmail).
        public int? FamilyId { get; set; }

        // Only populated by sp_PurchaseOrderLine_GetEffective — true when the latest APPLIED
        // rectification for this line was a full cancellation. Quantity/UnitPrice/CurrencyCode
        // still reflect the last real values in that case (a cancelled line contributes nothing
        // to totals, but its historical unit price/quantity remain visible for the audit trail).
        // Always false via sp_PurchaseOrderLine_GetByPurchaseOrderId (the raw, never-rectified read).
        public bool IsCancelled { get; set; }
    }
}
