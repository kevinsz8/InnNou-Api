namespace InnNou.Domain.Dtos
{
    public class ArticlePackagingLevelDto
    {
        public Guid ArticlePackagingLevelToken { get; set; }
        public int SequenceOrder { get; set; }
        public int UnitOfMeasureId { get; set; }
        public string? UnitOfMeasureCode { get; set; }
        public string? UnitOfMeasureSymbol { get; set; }
        public decimal QuantityInParentUnit { get; set; }
        public bool IsDefinedUnit { get; set; }
    }
}
