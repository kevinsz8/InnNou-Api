namespace InnNou.Application.Responses.Common
{
    public class ArticlePackagingConversion
    {
        public Guid ArticleToken { get; set; }
        public string Name { get; set; } = default!;
        public string? PurchaseUnitCode { get; set; }
        public Dictionary<string, string>? PurchaseUnitNameTranslations { get; set; }
        public List<ArticlePackagingConversionLevel> Levels { get; set; } = [];
    }

    public class ArticlePackagingConversionLevel
    {
        public int SequenceOrder { get; set; }
        public string? UnitCode { get; set; }
        public Dictionary<string, string>? UnitNameTranslations { get; set; }
        public decimal QuantityPerPurchaseUnit { get; set; }
        public bool IsDefinedUnit { get; set; }
    }
}
