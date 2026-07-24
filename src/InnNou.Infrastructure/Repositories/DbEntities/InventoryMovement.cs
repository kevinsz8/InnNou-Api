using InnNou.Application.Common;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class InventoryMovement
    {
        public int InventoryMovementId { get; set; }
        public Guid InventoryMovementToken { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public InventoryMovementType Type { get; set; }

        // Signed: + increase, - decrease.
        public decimal Quantity { get; set; }

        public Guid? GoodsReceiptToken { get; set; }
        public Guid? InventoryTransferToken { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
