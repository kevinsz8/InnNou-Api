namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class TaxRate
    {
        public int TaxRateId { get; set; }
        public Guid TaxRateToken { get; set; }
        public int TaxJurisdictionId { get; set; }
        public int TaxCategoryId { get; set; }
        public string? TaxCategoryCode { get; set; }
        public decimal RatePercent { get; set; }
        public bool IsActive { get; set; }
    }

    // sp_TaxRate_GetAllWithJurisdictionAndCategory's shape — every (Jurisdiction, Category)
    // combination, LEFT JOINed to TaxRates so a still-unconfigured pair (e.g. ES_CEUTA/
    // ES_MELILLA) shows up with a NULL rate instead of being silently absent from the grid.
    public class TaxRateGridRow
    {
        public int TaxJurisdictionId { get; set; }
        public Guid TaxJurisdictionToken { get; set; }
        public string TaxJurisdictionCode { get; set; } = default!;
        public string TaxJurisdictionName { get; set; } = default!;
        public int TaxCategoryId { get; set; }
        public Guid TaxCategoryToken { get; set; }
        public string TaxCategoryCode { get; set; } = default!;
        public int? TaxRateId { get; set; }
        public Guid? TaxRateToken { get; set; }
        public decimal? RatePercent { get; set; }
    }

    // sp_Article_GetEffectiveTaxCategoryByIds' batch-resolve result — internal to
    // PurchaseOrderService.CreateGoodsReceiptAsync, never mapped to a DTO.
    public class ArticleEffectiveTaxCategory
    {
        public int ArticleId { get; set; }
        public int? TaxCategoryId { get; set; }
        public string? TaxCategoryCode { get; set; }
    }
}
