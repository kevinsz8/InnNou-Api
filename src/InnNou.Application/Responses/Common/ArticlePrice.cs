namespace InnNou.Application.Responses.Common
{
    public class ArticlePrice
    {
        public Guid ArticlePriceToken { get; set; }
        public Guid ArticleToken { get; set; }
        public Guid? OrganizationToken { get; set; }
        public decimal Price { get; set; }
        public string CurrencyCode { get; set; } = default!;
        public DateTime EffectiveDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
