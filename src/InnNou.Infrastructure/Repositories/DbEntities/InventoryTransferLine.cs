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

        // Always positive — the amount moved From -> To.
        public decimal Quantity { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
