namespace InnNou.Application.Responses.Common
{
    public class InventoryMovement
    {
        public Guid InventoryMovementToken { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public string Type { get; set; } = default!;
        public decimal Quantity { get; set; }
        public string? EnteredUnitCode { get; set; }
        public decimal? EnteredQuantity { get; set; }
        public string? DefinedUnitCode { get; set; }
        public Dictionary<string, string>? DefinedUnitNameTranslations { get; set; }
        public decimal? DefinedUnitQuantity { get; set; }
        public Guid? GoodsReceiptToken { get; set; }
        public Guid? InventoryTransferToken { get; set; }
        public Guid? InventoryPeriodCountToken { get; set; }
        public Guid? InternalOrderShipmentToken { get; set; }
        public Guid? InternalOrderReceiptToken { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
