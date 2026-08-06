using InnNou.Application.Common;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Article
    {
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public SupplierType? SupplierType { get; set; }
        public string Name { get; set; } = default!;
        public string NormalizedName { get; set; } = default!;
        public string? Description { get; set; }
        public string? SupplierSku { get; set; }
        public string? Barcode { get; set; }
        public string? Brand { get; set; }
        public int? FamilyId { get; set; }
        public string? FamilyCode { get; set; }

        // Raw JSON text, parsed at the mapping layer — see Family.cs and
        // .claude/CatalogTranslationsModule.md.
        public string? FamilyNameTranslations { get; set; }
        public int? SubFamilyId { get; set; }
        public string? SubFamilyCode { get; set; }
        public string? SubFamilyNameTranslations { get; set; }
        public int PurchaseUnitId { get; set; }
        public Guid PurchaseUnitToken { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public string? PurchaseUnitSymbol { get; set; }
        public string? PurchaseUnitNameTranslations { get; set; }
        public decimal? MinimumOrderQty { get; set; }
        public int? LeadTimeDays { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public int? ReplacedByArticleId { get; set; }
        public Guid? ReplacedByArticleToken { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsInherited { get; set; }
        public string? FavoriteOrganizationName { get; set; }
        public int? CategoryId { get; set; }
        public Guid? CategoryToken { get; set; }
        public string? CategoryCode { get; set; }
        public string? CategoryNameTranslations { get; set; }
        public int? SubCategoryId { get; set; }
        public Guid? SubCategoryToken { get; set; }
        public string? SubCategoryCode { get; set; }
        public string? SubCategoryNameTranslations { get; set; }
        public bool IsCategoryInherited { get; set; }
        public string? ClassificationOrganizationName { get; set; }
        public int? TaxCategoryId { get; set; }
        public Guid? TaxCategoryToken { get; set; }
        public string? TaxCategoryCode { get; set; }
        public int? EffectiveTaxCategoryId { get; set; }
        public string? EffectiveTaxCategoryCode { get; set; }

        // Pure pre-fill convenience for Goods Receipts' own unit picker — never a lock, the
        // receiver can always pick a different valid unit for a specific receipt. Null means
        // "default to PurchaseUnitId" (today's behavior). See
        // migrations/20260806_Articles_AddDefaultReceivingUnit.sql.
        public int? DefaultReceivingUnitId { get; set; }
        public Guid? DefaultReceivingUnitToken { get; set; }
        public string? DefaultReceivingUnitCode { get; set; }
        public string? DefaultReceivingUnitNameTranslations { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
        public DateTime? DeletedUtc { get; set; }
        public string? DeletedBy { get; set; }
    }
}
