namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class SupplierReturnLine
    {
        public int SupplierReturnLineId { get; set; }
        public Guid SupplierReturnLineToken { get; set; }
        public int SupplierReturnId { get; set; }
        public int GoodsReceiptLineId { get; set; }
        public Guid GoodsReceiptLineToken { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public decimal QuantityRejected { get; set; }
        public string? RejectionReason { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
