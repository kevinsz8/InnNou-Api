namespace InnNou.Domain.Dtos
{
    // One line requested as part of a single GoodsReceipt create call. At least one of the 3
    // quantities must be > 0. QuantityAccepted is capped against the PurchaseOrderLine's
    // remaining-to-receive (service layer); QuantityCourtesy/QuantityRejected are uncapped by
    // design — a supplier-gifted surplus or a damaged/wrong item can exceed what was ordered
    // without ever being silently counted as billable.
    public class CreateGoodsReceiptLineInputDto
    {
        public Guid PurchaseOrderLineToken { get; set; }
        public decimal QuantityAccepted { get; set; }
        public decimal QuantityCourtesy { get; set; }
        public decimal QuantityRejected { get; set; }

        // Shared unit for all 3 quantities above — null means they're already denominated in the
        // article's own PurchaseUnitId (default, backward compatible). See
        // migrations/20260806_GoodsReceiptLine_UnitConversion.sql.
        public Guid? UnitToken { get; set; }

        public string? RejectionReason { get; set; }
        public string? LotNumber { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? SerialNumber { get; set; }
        public string? Notes { get; set; }
    }
}
