namespace InnNou.Domain.Dtos
{
    public class RequisitionIssueLineDto
    {
        public Guid RequisitionIssueLineToken { get; set; }
        public Guid RequisitionLineToken { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? PurchaseUnitCode { get; set; }

        public decimal QuantityIssued { get; set; }

        public string? IssuedUnitCode { get; set; }
        public decimal? IssuedQuantity { get; set; }

        // Secondary reference computed at read time (never stored) — see
        // ArticleUnitConversion.GetDefinedUnitEquivalent.
        public string? DefinedUnitCode { get; set; }
        public Dictionary<string, string>? DefinedUnitNameTranslations { get; set; }
        public decimal? DefinedUnitQuantity { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
