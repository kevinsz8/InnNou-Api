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
        public int PurchaseUnitId { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public InventoryMovementType Type { get; set; }

        // Signed: + increase, - decrease.
        public decimal Quantity { get; set; }

        // NULL means "entered directly in the article's PurchaseUnitId" — see
        // ArticleUnitConversion. When set (Adjustments/Transfers entered in a different unit, or
        // copied forward from a Requisition issue's own entered unit), EnteredQuantity carries
        // the same sign convention as Quantity above (signed, matching increase/decrease).
        public int? EnteredUnitId { get; set; }
        public string? EnteredUnitCode { get; set; }
        public decimal? EnteredQuantity { get; set; }

        public Guid? GoodsReceiptToken { get; set; }
        public Guid? InventoryTransferToken { get; set; }
        public Guid? InventoryPeriodCountToken { get; set; }
        public Guid? InternalOrderShipmentToken { get; set; }
        public Guid? InternalOrderReceiptToken { get; set; }
        public Guid? RequisitionIssueToken { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
