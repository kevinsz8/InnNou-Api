namespace InnNou.Domain.Dtos
{
    public class ArticleDto
    {
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
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
        public decimal PurchaseQuantity { get; set; }
        public int ContentUnitId { get; set; }
        public string? ContentUnitCode { get; set; }
        public string? ContentUnitSymbol { get; set; }
        public decimal? ContentQuantity { get; set; }
        public int? BaseUnitId { get; set; }
        public string? BaseUnitCode { get; set; }
        public string? BaseUnitSymbol { get; set; }
        public decimal? MinimumOrderQty { get; set; }
        public int? LeadTimeDays { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }
}
