namespace InnNou.Application.Responses.Common
{
    public class Article
    {
        public Guid ArticleToken { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierType { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string? SupplierSku { get; set; }
        public string? Barcode { get; set; }
        public string? Brand { get; set; }
        public int? FamilyId { get; set; }
        public string? FamilyCode { get; set; }
        public int? SubFamilyId { get; set; }
        public string? SubFamilyCode { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public string? PurchaseUnitSymbol { get; set; }
        public List<ArticlePackagingLevel> PackagingLevels { get; set; } = [];
        public decimal? MinimumOrderQty { get; set; }
        public int? LeadTimeDays { get; set; }
        public bool IsActive { get; set; }
        public Guid? ReplacedByArticleToken { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsInherited { get; set; }
        public string? FavoriteOrganizationName { get; set; }
    }
}
