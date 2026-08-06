namespace InnNou.Domain.Dtos
{
    public class SupplierCreditNoteDto
    {
        public Guid SupplierCreditNoteToken { get; set; }

        public Guid SupplierReturnToken { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public string? PurchaseOrderNumber { get; set; }

        public Guid OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }
        public Guid SupplierToken { get; set; }
        public string? SupplierName { get; set; }

        public string CreditNoteNumber { get; set; } = default!;
        public string InternalSequentialNumber { get; set; } = default!;
        public DateTime CreditNoteDate { get; set; }
        public string Reason { get; set; } = default!;
        public string? Notes { get; set; }

        public List<SupplierCreditNoteLineDto> Lines { get; set; } = [];
        public List<SupplierCreditNoteTaxBreakdownDto> TaxBreakdown { get; set; } = [];
        // Auto-detected SupplierInvoices this note corrects — empty when the credited quantity
        // was never invoiced (the common case, since InnNou only ever invoices QuantityAccepted).
        public List<SupplierCreditNoteInvoiceRefDto> CorrectedInvoices { get; set; } = [];

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        public int LineCount { get; set; }
        public decimal? TotalAmount { get; set; }
    }

    public class SupplierCreditNoteLineDto
    {
        public Guid SupplierCreditNoteLineToken { get; set; }
        public Guid SupplierReturnLineToken { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public decimal QuantityCredited { get; set; }
        public decimal UnitPrice { get; set; }
        public string CurrencyCode { get; set; } = default!;
        public string? TaxCategoryCode { get; set; }
        public decimal? TaxRatePercent { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool WasManuallyEntered { get; set; }
    }

    public class SupplierCreditNoteTaxBreakdownDto
    {
        public decimal TaxRatePercent { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string CurrencyCode { get; set; } = default!;
    }

    public class SupplierCreditNoteInvoiceRefDto
    {
        public Guid SupplierInvoiceToken { get; set; }
        public string InternalSequentialNumber { get; set; } = default!;
        public string? SupplierInvoiceNumber { get; set; }
    }

    // One input row per SupplierReturnLine being credited — UnitPrice is optional: omitted, it
    // defaults to the underlying GoodsReceiptLine's own frozen UnitPrice; required (and flagged
    // WasManuallyEntered) only when that line predates the freeze fix and has none.
    public class CreateSupplierCreditNoteLineInputDto
    {
        public Guid SupplierReturnLineToken { get; set; }
        public decimal? UnitPrice { get; set; }
    }
}
