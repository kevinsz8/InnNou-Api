namespace InnNou.Infrastructure.Repositories.DbEntities
{
    // sp_SupplierInvoicePurchaseOrder_GetBySupplierInvoiceId's row shape — which PurchaseOrders
    // one SupplierInvoice consolidates.
    public class SupplierInvoicePurchaseOrder
    {
        public int PurchaseOrderId { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public string PurchaseOrderNumber { get; set; } = default!;
    }
}
