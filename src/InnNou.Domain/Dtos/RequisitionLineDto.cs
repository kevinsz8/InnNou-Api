namespace InnNou.Domain.Dtos
{
    public class RequisitionLineDto
    {
        public Guid RequisitionLineToken { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? PurchaseUnitCode { get; set; }

        public decimal QuantityRequested { get; set; }
        public decimal QuantityIssued { get; set; }

        public string? RequestedUnitCode { get; set; }
        public decimal? RequestedQuantity { get; set; }

        // Secondary reference computed at read time (never stored) — see
        // ArticleUnitConversion.GetDefinedUnitEquivalent. Null when the effective quantity is
        // already denominated in the article's own Unidad Definida (nothing useful to add).
        public string? DefinedUnitCode { get; set; }
        public Dictionary<string, string>? DefinedUnitNameTranslations { get; set; }
        public decimal? DefinedUnitQuantity { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
