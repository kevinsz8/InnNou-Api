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

        // Live-resolved (not frozen) — same reasoning as StockLevel/OrderLine's own Family/
        // SubFamily/Category/SubCategory: this only backs search/filter within one period's
        // own line list.
        public string? FamilyCode { get; set; }
        public string? SubFamilyCode { get; set; }
        public string? CategoryCode { get; set; }
        public string? SubCategoryCode { get; set; }

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
