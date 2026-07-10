namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class ArticlePrice
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
    }
}
