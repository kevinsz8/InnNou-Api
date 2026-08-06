namespace InnNou.Application.Responses.Common
{
    public class ArticlePriceComparison
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
        public string? NormalizedUnitCode { get; set; }
        public Dictionary<string, string>? NormalizedUnitNameTranslations { get; set; }
        public decimal? PricePerNormalizedUnit { get; set; }
        public bool IsComparable { get; set; }
        public bool IsCheapest { get; set; }
    }
}
