namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class GoodsReceiptLine
    {
        public int GoodsReceiptLineId { get; set; }
        public Guid GoodsReceiptLineToken { get; set; }
        public int GoodsReceiptId { get; set; }
        public int PurchaseOrderLineId { get; set; }
        public Guid PurchaseOrderLineToken { get; set; }

        // The line's originally ordered quantity (PurchaseOrderLine.Quantity, not the
        // rectification-effective value) — purely a display convenience denormalization.
        public decimal OrderedQuantity { get; set; }

        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }

        public decimal QuantityAccepted { get; set; }
        public decimal QuantityCourtesy { get; set; }
        public decimal QuantityRejected { get; set; }
        public string? RejectionReason { get; set; }
        public string? LotNumber { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? SerialNumber { get; set; }
        public string? Notes { get; set; }

        public int? TaxCategoryId { get; set; }
        public string? TaxCategoryCode { get; set; }
        public decimal? TaxRatePercent { get; set; }
        public decimal? TaxableAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? TotalAmount { get; set; }

        // The line's own PurchaseOrderLine.PurchaseUnitId/Code, denormalized purely so the
        // canonical Accepted/Courtesy/Rejected quantities have a unit label to display when no
        // EnteredUnitId was captured — same reasoning as InventoryMovement's own PurchaseUnitCode.
        public int PurchaseUnitId { get; set; }
        public string? PurchaseUnitCode { get; set; }

        // Shared across Accepted/Courtesy/Rejected — a receiver counts all three from the same
        // opened container in the same unit. NULL means every quantity below was entered directly
        // in the Purchase Unit. See migrations/20260806_GoodsReceiptLine_UnitConversion.sql.
        public int? EnteredUnitId { get; set; }
        public string? EnteredUnitCode { get; set; }
        public string? EnteredUnitNameTranslations { get; set; }
        public decimal? AcceptedQuantityInUnit { get; set; }
        public decimal? CourtesyQuantityInUnit { get; set; }
        public decimal? RejectedQuantityInUnit { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
