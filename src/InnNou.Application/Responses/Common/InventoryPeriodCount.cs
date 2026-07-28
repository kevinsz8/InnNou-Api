namespace InnNou.Application.Responses.Common
{
    public class InventoryPeriodCount
    {
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
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
