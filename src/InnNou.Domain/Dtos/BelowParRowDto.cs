namespace InnNou.Domain.Dtos
{
    public class BelowParRowDto
    {
        public Guid ParLevelToken { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public int? LeadTimeDays { get; set; }
        public string? SupplierName { get; set; }
        public string? PurchaseUnitCode { get; set; }

        public decimal QuantityOnHand { get; set; }
        public decimal EffectiveMinimumQuantity { get; set; }
        public decimal EffectiveReorderQuantity { get; set; }

        // "BASE" / "SEASONAL" / "EVENT".
        public string EffectiveSource { get; set; } = default!;
        public string? OverrideLabel { get; set; }
    }
}
