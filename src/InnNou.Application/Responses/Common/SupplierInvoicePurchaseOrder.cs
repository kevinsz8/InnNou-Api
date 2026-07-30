namespace InnNou.Application.Responses.Common
{
    public class SupplierInvoicePurchaseOrder
    {
        public Guid PurchaseOrderToken { get; set; }
        public string PurchaseOrderNumber { get; set; } = default!;
    }
}
