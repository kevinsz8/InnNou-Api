namespace InnNou.Infrastructure.Repositories.DbEntities
{
    // Result shape of sp_ParLevel_GetBelowPar — a distinct read shape (joins StockLevels/Article/
    // Supplier on top of the effective-resolution columns), not a subclass of ParLevel/StockLevel.
    public class ParLevelBelowParRow
    {
        public int ParLevelId { get; set; }
        public Guid ParLevelToken { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public int OrganizationId { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public int? LeadTimeDays { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int PurchaseUnitId { get; set; }
        public string? PurchaseUnitCode { get; set; }

        // Live-resolved (not frozen) — same reasoning as StockLevel/OrderLine's own Family/
        // SubFamily/Category/SubCategory: this only backs search/filter.
        public string? FamilyCode { get; set; }
        public string? SubFamilyCode { get; set; }
        public string? CategoryCode { get; set; }
        public string? SubCategoryCode { get; set; }

        public decimal QuantityOnHand { get; set; }
        public decimal EffectiveMinimumQuantity { get; set; }
        public decimal EffectiveReorderQuantity { get; set; }

        // "BASE" / "SEASONAL" / "EVENT".
        public string EffectiveSource { get; set; } = default!;
        public string? OverrideLabel { get; set; }

        public int TotalCount { get; set; }
    }
}
