namespace InnNou.Domain.Dtos
{
    // One line-level correction requested as part of a single PurchaseOrderRectification.
    // Cancel=true means the line is cancelled outright (NewQuantity/NewUnitPrice/NewCurrencyCode
    // are ignored); otherwise NewQuantity/NewUnitPrice are required and NewCurrencyCode is
    // optional (falls back to the line's current effective currency when omitted).
    public class RectifyPurchaseOrderLineInputDto
    {
        public Guid PurchaseOrderLineToken { get; set; }
        public bool Cancel { get; set; }
        public decimal? NewQuantity { get; set; }
        public decimal? NewUnitPrice { get; set; }
        public string? NewCurrencyCode { get; set; }
    }
}
