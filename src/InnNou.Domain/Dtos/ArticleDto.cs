namespace InnNou.Domain.Dtos
{
    public class ArticleDto
    {
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierType { get; set; }
        public string Name { get; set; } = default!;
        public string NormalizedName { get; set; } = default!;
        public string? Description { get; set; }
        public string? SupplierSku { get; set; }
        public string? Barcode { get; set; }
        public string? Brand { get; set; }
        public int? FamilyId { get; set; }
        public string? FamilyCode { get; set; }
        public int? SubFamilyId { get; set; }
        public string? SubFamilyCode { get; set; }
        public int PurchaseUnitId { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public string? PurchaseUnitSymbol { get; set; }

        // The packaging chain: N ordered levels (SequenceOrder 1 = closest to
        // PurchaseUnitId), exactly one flagged IsDefinedUnit = true — always the
        // last (highest SequenceOrder) level. Populated by GetByTokenAsync via a
        // second query (same pattern as OrderTemplateDto.Lines); left empty by
        // GetPagedAsync to avoid N+1 on list views. Structurally immutable once
        // created — changing it requires Supersede, same as PurchaseUnitId.
        public List<ArticlePackagingLevelDto> PackagingLevels { get; set; } = [];

        public decimal? MinimumOrderQty { get; set; }
        public int? LeadTimeDays { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public int? ReplacedByArticleId { get; set; }
        public Guid? ReplacedByArticleToken { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsInherited { get; set; }
        public string? FavoriteOrganizationName { get; set; }

        // Effective Category/SubCategory classification (own-organization row wins, else
        // inherited from the nearest Super Asociado ancestor) — same "own ∪ inherited"
        // resolution as IsFavorite/IsInherited above, denormalized from ArticleClassifications
        // via the same @OrganizationId param. Null when the article has no classification at
        // all (most articles, initially). SubCategoryId is optional even when CategoryId is set.
        public int? CategoryId { get; set; }
        public Guid? CategoryToken { get; set; }
        public string? CategoryCode { get; set; }
        public int? SubCategoryId { get; set; }
        public Guid? SubCategoryToken { get; set; }
        public string? SubCategoryCode { get; set; }
        public bool IsCategoryInherited { get; set; }
        public string? ClassificationOrganizationName { get; set; }
    }
}
