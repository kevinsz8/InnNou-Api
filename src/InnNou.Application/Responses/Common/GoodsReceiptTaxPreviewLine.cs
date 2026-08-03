namespace InnNou.Application.Responses.Common
{
    public class GoodsReceiptTaxPreviewLine
    {
        public Guid PurchaseOrderLineToken { get; set; }
        public string? TaxCategoryCode { get; set; }
        public decimal? TaxRatePercent { get; set; }
    }
}
