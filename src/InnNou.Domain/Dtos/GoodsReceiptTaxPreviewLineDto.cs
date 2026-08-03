namespace InnNou.Domain.Dtos
{
    // One PurchaseOrderLine's effective tax category/rate, resolved the exact same way
    // CreateGoodsReceiptAsync resolves it for real (Article override -> FamilyTaxCategoryOverride
    // for the warehouse's jurisdiction -> Family default, then TaxRates for that jurisdiction),
    // but as a read-only PREVIEW before the receipt is actually submitted - never throws on
    // missing config, just leaves TaxCategoryCode/TaxRatePercent null so the receiving page can
    // show "-" instead of blocking. The real submission still re-validates and throws for real.
    public class GoodsReceiptTaxPreviewLineDto
    {
        public Guid PurchaseOrderLineToken { get; set; }
        public string? TaxCategoryCode { get; set; }
        public decimal? TaxRatePercent { get; set; }
    }
}
