namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class SupplierInvoice
    {
        public int SupplierInvoiceId { get; set; }
        public Guid SupplierInvoiceToken { get; set; }
        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }
        public int SupplierId { get; set; }
        public Guid SupplierToken { get; set; }
        public string? SupplierName { get; set; }
        public string SupplierInvoiceNumber { get; set; } = default!;
        public string InternalSequentialNumber { get; set; } = default!;
        public DateTime InvoiceDate { get; set; }
        public string Status { get; set; } = default!;
        public string? AttachmentUrl { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        // Only populated by sp_SupplierInvoice_GetPaged — cheap aggregation, not per-row N+1.
        public int LineCount { get; set; }
        public decimal? TotalTaxableAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? PurchaseOrderNumbers { get; set; }
        public string? WarehouseNames { get; set; }
    }
}
