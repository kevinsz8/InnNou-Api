namespace InnNou.Application.Responses.Common
{
    public class BelowParRow
    {
        public Guid ParLevelToken { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public int? LeadTimeDays { get; set; }
        public string? SupplierName { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public string? FamilyCode { get; set; }
        public string? SubFamilyCode { get; set; }
        public string? CategoryCode { get; set; }
        public string? SubCategoryCode { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal EffectiveMinimumQuantity { get; set; }
        public decimal EffectiveReorderQuantity { get; set; }
        public string EffectiveSource { get; set; } = default!;
        public string? OverrideLabel { get; set; }
    }
}
