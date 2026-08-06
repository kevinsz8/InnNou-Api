namespace InnNou.Domain.Dtos
{
    public class ArticlePackagingConversionDto
    {
        public Guid ArticleToken { get; set; }
        public string Name { get; set; } = default!;
        public string? PurchaseUnitCode { get; set; }
        public Dictionary<string, string>? PurchaseUnitNameTranslations { get; set; }
        public List<ArticlePackagingConversionLevelDto> Levels { get; set; } = [];
    }

    public class ArticlePackagingConversionLevelDto
    {
        public int SequenceOrder { get; set; }
        public string? UnitCode { get; set; }
        public Dictionary<string, string>? UnitNameTranslations { get; set; }
        public decimal QuantityPerPurchaseUnit { get; set; }
        public bool IsDefinedUnit { get; set; }
    }
}
