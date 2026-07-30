namespace InnNou.Domain.Dtos
{
    public class SupplierInvoicePurchaseOrderDto
    {
        public Guid PurchaseOrderToken { get; set; }
        public string PurchaseOrderNumber { get; set; } = default!;
    }
}
