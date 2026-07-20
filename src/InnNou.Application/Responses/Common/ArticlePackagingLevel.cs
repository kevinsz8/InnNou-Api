namespace InnNou.Application.Responses.Common
{
    public class ArticlePackagingLevel
    {
        public Guid ArticlePackagingLevelToken { get; set; }
        public int SequenceOrder { get; set; }
        public string? UnitOfMeasureCode { get; set; }
        public string? UnitOfMeasureSymbol { get; set; }
        public decimal QuantityInParentUnit { get; set; }
        public bool IsDefinedUnit { get; set; }
    }
}
