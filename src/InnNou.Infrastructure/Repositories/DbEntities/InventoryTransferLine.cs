namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class InventoryTransferLine
    {
        public int InventoryTransferLineId { get; set; }
        public Guid InventoryTransferLineToken { get; set; }
        public int InventoryTransferId { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public int PurchaseUnitId { get; set; }
        public string? PurchaseUnitCode { get; set; }

        // Always positive — the amount moved From -> To.
        public decimal Quantity { get; set; }

        // NULL means "entered directly in PurchaseUnitId" — see ArticleUnitConversion. When set,
        // these record what the user actually typed for accurate re-display; Quantity above
        // always stays the PurchaseUnitId-normalized value every other consumer expects.
        public int? TransferredUnitId { get; set; }
        public string? TransferredUnitCode { get; set; }
        public decimal? TransferredQuantity { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
