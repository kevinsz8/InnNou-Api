namespace InnNou.Domain.Dtos
{
    public class ArticleDiscountDto
    {
        public int ArticleDiscountId { get; set; }
        public Guid ArticleDiscountToken { get; set; }
        public int SupplierId { get; set; }
        public Guid SupplierToken { get; set; }
        public string SupplierName { get; set; } = default!;
        public int? ArticleId { get; set; }
        public Guid? ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public int? SubFamilyId { get; set; }
        public Guid? SubFamilyToken { get; set; }
        public string? SubFamilyCode { get; set; }
        public int? FamilyId { get; set; }
        public Guid? FamilyToken { get; set; }
        public string? FamilyCode { get; set; }
        public int DiscountTypeId { get; set; }
        public string DiscountTypeCode { get; set; } = default!;
        public decimal DiscountValue { get; set; }
        public string? CurrencyCode { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveUntil { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
