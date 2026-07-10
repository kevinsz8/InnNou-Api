namespace InnNou.Domain.Dtos
{
    public class ArticlePriceDto
    {
        public int ArticlePriceId { get; set; }
        public Guid ArticlePriceToken { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public int? OrganizationId { get; set; }
        public Guid? OrganizationToken { get; set; }
        public decimal Price { get; set; }
        public string CurrencyCode { get; set; } = default!;
        public DateTime EffectiveDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        // Transient, write-only: the owning Article's SupplierId, resolved by the caller (handler)
        // before CreateAsync so the service can authorize without a second DB round trip.
        // Same pattern as SupplierDto.LoginEmail/Password — never round-trips in a response mapping.
        public int SupplierId { get; set; }
    }
}
