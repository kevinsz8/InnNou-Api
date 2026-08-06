namespace InnNou.Application.Responses.Common
{
    public class ArticlePackagingLevel
    {
        public Guid ArticlePackagingLevelToken { get; set; }
        public int SequenceOrder { get; set; }
        // Added so a caller (e.g. a Requisition/Inventory quantity-entry unit picker) can send
        // this level's own unit straight back as a UnitToken — see ArticleUnitConversion.
        public Guid UnitOfMeasureToken { get; set; }
        public string? UnitOfMeasureCode { get; set; }
        public string? UnitOfMeasureSymbol { get; set; }
        public Dictionary<string, string>? UnitOfMeasureNameTranslations { get; set; }
        public decimal QuantityInParentUnit { get; set; }
        public bool IsDefinedUnit { get; set; }
    }
}
