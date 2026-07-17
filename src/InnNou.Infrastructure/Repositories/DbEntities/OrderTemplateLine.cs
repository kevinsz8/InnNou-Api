namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class OrderTemplateLine
    {
        public int OrderTemplateLineId { get; set; }
        public Guid OrderTemplateLineToken { get; set; }
        public int OrderTemplateId { get; set; }
        public Guid OrderTemplateToken { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierSku { get; set; }
        public string? SupplierType { get; set; }

        public int PurchaseUnitId { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public string? PurchaseUnitSymbol { get; set; }

        // Never filtered out server-side — a stale line (its Article since deactivated,
        // soft-deleted, or superseded) must still surface so the edit page can badge it
        // instead of silently vanishing. See sp_OrderTemplateLine_GetByOrderTemplateId.
        public bool IsArticleActive { get; set; }
        public bool IsArticleDeleted { get; set; }
        public Guid? ReplacedByArticleToken { get; set; }

        public decimal Quantity { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
