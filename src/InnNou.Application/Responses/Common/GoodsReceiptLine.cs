namespace InnNou.Application.Responses.Common
{
    public class GoodsReceiptLine
    {
        public Guid GoodsReceiptLineToken { get; set; }
        public Guid PurchaseOrderLineToken { get; set; }
        public decimal OrderedQuantity { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public decimal QuantityAccepted { get; set; }
        public decimal QuantityCourtesy { get; set; }
        public decimal QuantityRejected { get; set; }
        public string? RejectionReason { get; set; }
        public string? LotNumber { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? SerialNumber { get; set; }
        public string? Notes { get; set; }
        public string? TaxCategoryCode { get; set; }
        public decimal? TaxRatePercent { get; set; }
        public decimal? TaxableAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public string? EnteredUnitCode { get; set; }
        public Dictionary<string, string>? EnteredUnitNameTranslations { get; set; }
        public decimal? AcceptedQuantityInUnit { get; set; }
        public decimal? CourtesyQuantityInUnit { get; set; }
        public decimal? RejectedQuantityInUnit { get; set; }
        public string? DefinedUnitCode { get; set; }
        public Dictionary<string, string>? DefinedUnitNameTranslations { get; set; }
        public decimal? AcceptedDefinedUnitQuantity { get; set; }
        public decimal? CourtesyDefinedUnitQuantity { get; set; }
        public decimal? RejectedDefinedUnitQuantity { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
