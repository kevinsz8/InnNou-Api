namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class SupplierCreditNote
    {
        public int SupplierCreditNoteId { get; set; }
        public Guid SupplierCreditNoteToken { get; set; }

        public int SupplierReturnId { get; set; }
        public Guid SupplierReturnToken { get; set; }
        public int PurchaseOrderId { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public int WarehouseId { get; set; }

        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }

        public int SupplierId { get; set; }
        public Guid SupplierToken { get; set; }
        public string? SupplierName { get; set; }

        public string CreditNoteNumber { get; set; } = default!;
        public string InternalSequentialNumber { get; set; } = default!;
        public DateTime CreditNoteDate { get; set; }
        public string Reason { get; set; } = default!;
        public string? Notes { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        // Only populated by sp_SupplierCreditNote_GetPaged.
        public int LineCount { get; set; }
        public decimal? TotalAmount { get; set; }
    }
}
