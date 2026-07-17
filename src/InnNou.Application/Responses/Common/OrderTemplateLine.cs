namespace InnNou.Application.Responses.Common
{
    public class OrderTemplateLine
    {
        public Guid OrderTemplateLineToken { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierSku { get; set; }
        public string? SupplierType { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public string? PurchaseUnitSymbol { get; set; }
        public bool IsArticleActive { get; set; }
        public bool IsArticleDeleted { get; set; }
        public Guid? ReplacedByArticleToken { get; set; }
        public decimal Quantity { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
