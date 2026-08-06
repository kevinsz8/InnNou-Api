namespace InnNou.Domain.Dtos
{
    // A candidate row for cross-supplier price comparison within one Category/SubCategory --
    // deliberately never claims two Articles ARE the same product, only that they're worth a
    // human buyer looking at side by side (see CLAUDE.md's "Article price comparison" section
    // for why: GTIN/Barcode only ever matches an identical pack size from the same source, which
    // structurally can't cover the cross-package-size comparisons that carry the real value).
    public class ArticlePriceComparisonDto
    {
        public Guid ArticleToken { get; set; }
        public string Name { get; set; } = default!;
        public string? SupplierName { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public Dictionary<string, string>? PurchaseUnitNameTranslations { get; set; }
        public decimal? Price { get; set; }
        public string? CurrencyCode { get; set; }
        public string? DefinedUnitCode { get; set; }
        public Dictionary<string, string>? DefinedUnitNameTranslations { get; set; }
        public decimal? DefinedUnitQuantityPerPurchaseUnit { get; set; }
        public decimal? PricePerDefinedUnit { get; set; }
        // The unit every comparable row in this response was normalized into -- the Defined Unit
        // of whichever Article in the group was resolved first. Null when this row's own Defined
        // Unit's UnitType has no other member in the group, or when Price never resolved.
        public string? NormalizedUnitCode { get; set; }
        public Dictionary<string, string>? NormalizedUnitNameTranslations { get; set; }
        public decimal? PricePerNormalizedUnit { get; set; }
        // False when Price never resolved, or no UnitConversionRate exists between this row's
        // own Defined Unit and the group's NormalizedUnitCode -- shown, but excluded from ranking.
        public bool IsComparable { get; set; }
        public bool IsCheapest { get; set; }
    }
}
