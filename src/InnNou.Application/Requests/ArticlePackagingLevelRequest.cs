namespace InnNou.Application.Requests
{
    public class ArticlePackagingLevelRequest
    {
        public int SequenceOrder { get; set; }
        public Guid UnitOfMeasureToken { get; set; }
        public decimal QuantityInParentUnit { get; set; }
        public bool IsDefinedUnit { get; set; }
    }
}
