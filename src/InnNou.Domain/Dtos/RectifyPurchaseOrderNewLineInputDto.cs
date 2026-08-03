namespace InnNou.Domain.Dtos
{
    // One brand-new line requested as part of a single PurchaseOrderRectification — an article
    // that was never on the original PurchaseOrder (e.g. the supplier shipped it against a
    // phone-in addition that never made it onto the formal order). ManualUnitPrice/CurrencyCode
    // are only used when the article has no resolvable catalog price (SERVICE/MIXED supplier),
    // same fallback OrderService.AddLineAsync already uses for a draft Order line.
    public class RectifyPurchaseOrderNewLineInputDto
    {
        public Guid ArticleToken { get; set; }
        public decimal Quantity { get; set; }
        public decimal? ManualUnitPrice { get; set; }
        public string? ManualCurrencyCode { get; set; }
    }
}
