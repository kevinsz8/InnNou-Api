namespace InnNou.Infrastructure.Repositories.DbEntities
{
    // A SupplierInvoice this credit note was auto-detected to correct — see the
    // SupplierCreditNoteInvoices join table's own migration comment for why this is never
    // user-picked.
    public class SupplierCreditNoteInvoiceRef
    {
        public int SupplierInvoiceId { get; set; }
        public Guid SupplierInvoiceToken { get; set; }
        public string InternalSequentialNumber { get; set; } = default!;
        public string? SupplierInvoiceNumber { get; set; }
    }
}
