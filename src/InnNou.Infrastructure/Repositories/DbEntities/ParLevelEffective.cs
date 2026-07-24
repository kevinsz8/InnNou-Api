namespace InnNou.Infrastructure.Repositories.DbEntities
{
    // Result shape of sp_ParLevel_GetEffective — "what applies today", not a persisted row.
    public class ParLevelEffective
    {
        public int ParLevelId { get; set; }
        public Guid ParLevelToken { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public int? LeadTimeDays { get; set; }

        public decimal BaseMinimumQuantity { get; set; }
        public decimal BaseReorderQuantity { get; set; }

        public decimal EffectiveMinimumQuantity { get; set; }
        public decimal EffectiveReorderQuantity { get; set; }

        // "BASE" / "SEASONAL" / "EVENT".
        public string EffectiveSource { get; set; } = default!;
        public Guid? EffectiveOverrideToken { get; set; }
        public string? EffectiveOverrideLabel { get; set; }
    }
}
