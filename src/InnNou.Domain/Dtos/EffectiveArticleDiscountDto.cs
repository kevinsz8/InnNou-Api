namespace InnNou.Domain.Dtos
{
    // Result of sp_ArticleDiscount_GetEffective — the resolved discount (if any) for one Article
    // as of a given date. Consumed by OrderService.AddLineAsync to freeze BaseUnitPrice/
    // DiscountTypeId/DiscountValue onto OrderLine at line-add time.
    public class EffectiveArticleDiscountDto
    {
        public Guid ArticleDiscountToken { get; set; }
        public int DiscountTypeId { get; set; }
        public string DiscountTypeCode { get; set; } = default!;
        public decimal DiscountValue { get; set; }
        public string? CurrencyCode { get; set; }
        public string ScopeLevel { get; set; } = default!;
    }
}
