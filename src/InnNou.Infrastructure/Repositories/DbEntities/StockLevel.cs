namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class StockLevel
    {
        public int StockLevelId { get; set; }
        public Guid StockLevelToken { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public int OrganizationId { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int PurchaseUnitId { get; set; }
        public string? PurchaseUnitCode { get; set; }

        // Denominated in Article.PurchaseUnitId — same unit every OrderLine/PurchaseOrderLine/
        // GoodsReceiptLine quantity already uses.
        public decimal QuantityOnHand { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
