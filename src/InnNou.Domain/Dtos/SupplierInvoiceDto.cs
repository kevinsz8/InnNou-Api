namespace InnNou.Domain.Dtos
{
    public class SupplierInvoiceDto
    {
        public Guid SupplierInvoiceToken { get; set; }
        public Guid OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }
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

        public int LineCount { get; set; }
        public decimal? TotalTaxableAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? PurchaseOrderNumbers { get; set; }

        public List<SupplierInvoiceLineDto> Lines { get; set; } = [];
        public List<SupplierInvoicePurchaseOrderDto> PurchaseOrders { get; set; } = [];
    }
}
