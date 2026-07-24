namespace InnNou.Domain.Dtos
{
    public class StockLevelDto
    {
        public Guid StockLevelToken { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? SupplierName { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public decimal QuantityOnHand { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
