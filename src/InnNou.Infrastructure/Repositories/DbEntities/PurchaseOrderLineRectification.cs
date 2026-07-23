namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class PurchaseOrderLineRectification
    {
        public int PurchaseOrderLineRectificationId { get; set; }
        public Guid PurchaseOrderLineRectificationToken { get; set; }
        public int PurchaseOrderRectificationId { get; set; }
        public int PurchaseOrderLineId { get; set; }
        public Guid PurchaseOrderLineToken { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string Action { get; set; } = default!;
        public decimal? PreviousQuantity { get; set; }
        public decimal? NewQuantity { get; set; }
        public decimal? PreviousUnitPrice { get; set; }
        public decimal? NewUnitPrice { get; set; }
        public string? PreviousCurrencyCode { get; set; }
        public string? NewCurrencyCode { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
