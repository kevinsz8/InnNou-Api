namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class ArticlePackagingLevel
    {
        public int ArticlePackagingLevelId { get; set; }
        public Guid ArticlePackagingLevelToken { get; set; }
        public int ArticleId { get; set; }
        public byte SequenceOrder { get; set; }
        public int UnitOfMeasureId { get; set; }
        public string? UnitOfMeasureCode { get; set; }
        public string? UnitOfMeasureSymbol { get; set; }
        public decimal QuantityInParentUnit { get; set; }
        public bool IsDefinedUnit { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
