namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class InventoryPeriodCount
    {
        public int InventoryPeriodCountId { get; set; }
        public Guid InventoryPeriodCountToken { get; set; }
        public int InventoryPeriodId { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }

        public decimal OpeningQuantity { get; set; }
        public decimal? CountedQuantity { get; set; }
        public decimal? SystemQuantityAtClose { get; set; }
        public decimal? VarianceQuantity { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
