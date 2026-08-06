namespace InnNou.Domain.Dtos
{
    public class GoodsReceiptLineDto
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

        // Frozen for every received line (not just billable ones) since 2026-08-07 — see
        // .claude/ArticleUnitConversionModule.md's "Price comparison report" section. Null for
        // lines received before that date.
        public decimal? UnitPrice { get; set; }
        public string? CurrencyCode { get; set; }

        // Computed and frozen at receipt time (PurchaseOrderService.CreateGoodsReceiptAsync) —
        // null for lines received before the Tax module existed. See .claude/GoodsReceiptsModule.md.
        public string? TaxCategoryCode { get; set; }
        public decimal? TaxRatePercent { get; set; }
        public decimal? TaxableAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? TotalAmount { get; set; }

        public string? PurchaseUnitCode { get; set; }

        // Shared across the 3 quantities below — see GoodsReceiptLine (Infrastructure entity) for
        // the "one unit per line" reasoning.
        public string? EnteredUnitCode { get; set; }
        public Dictionary<string, string>? EnteredUnitNameTranslations { get; set; }
        public decimal? AcceptedQuantityInUnit { get; set; }
        public decimal? CourtesyQuantityInUnit { get; set; }
        public decimal? RejectedQuantityInUnit { get; set; }

        // Secondary "how much is that in the article's own Unidad Definida" reference — computed
        // per-quantity (batched, see PurchaseOrderService.CreateGoodsReceiptAsync/GetGoodsReceiptsAsync)
        // since Accepted/Courtesy/Rejected can differ even though they share one EnteredUnitId.
        // Code/NameTranslations are shared (same article, same Unidad Definida); each null when
        // there's nothing useful to add for that particular quantity (see ArticleUnitConversion.
        // GetDefinedUnitEquivalent).
        public string? DefinedUnitCode { get; set; }
        public Dictionary<string, string>? DefinedUnitNameTranslations { get; set; }
        public decimal? AcceptedDefinedUnitQuantity { get; set; }
        public decimal? CourtesyDefinedUnitQuantity { get; set; }
        public decimal? RejectedDefinedUnitQuantity { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
