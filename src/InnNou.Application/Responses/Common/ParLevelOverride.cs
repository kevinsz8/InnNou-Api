namespace InnNou.Application.Responses.Common
{
    public class ParLevelOverride
    {
        public Guid ParLevelOverrideToken { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string Type { get; set; } = default!;
        public string? Label { get; set; }
        public decimal MinimumQuantity { get; set; }
        public decimal ReorderQuantity { get; set; }
        public int? StartMonth { get; set; }
        public int? StartDay { get; set; }
        public int? EndMonth { get; set; }
        public int? EndDay { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
