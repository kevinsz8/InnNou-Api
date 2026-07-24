namespace InnNou.Domain.Dtos
{
    public class InventoryMovementDto
    {
        public Guid InventoryMovementToken { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string Type { get; set; } = default!;
        public decimal Quantity { get; set; }
        public Guid? GoodsReceiptToken { get; set; }
        public Guid? InventoryTransferToken { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
