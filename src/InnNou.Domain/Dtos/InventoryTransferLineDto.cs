namespace InnNou.Domain.Dtos
{
    public class InventoryTransferLineDto
    {
        public Guid InventoryTransferLineToken { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public decimal Quantity { get; set; }
        public string? TransferredUnitCode { get; set; }
        public decimal? TransferredQuantity { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
