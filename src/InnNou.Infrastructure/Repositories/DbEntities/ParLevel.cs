namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class ParLevel
    {
        public int ParLevelId { get; set; }
        public Guid ParLevelToken { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public int OrganizationId { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }

        // Denominated in Article.PurchaseUnitId — same unit StockLevels tracks.
        public decimal MinimumQuantity { get; set; }
        public decimal ReorderQuantity { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
