namespace InnNou.Domain.Dtos
{
    // One line in a batch AddOrderLinesCommandRequest — same fields as
    // AddOrderLineCommandRequest, minus OrderToken (shared across the whole batch).
    public class AddOrderLineInputDto
    {
        public Guid ArticleToken { get; set; }
        public decimal Quantity { get; set; }

        // Only honored when the article's supplier is SERVICE/MIXED and it has no catalog
        // ArticlePrice — see CLAUDE.md's "Supplier type" section. Ignored (and not required) for
        // PRODUCT suppliers, which must resolve a real catalog price.
        public decimal? ManualUnitPrice { get; set; }
        public string? ManualCurrencyCode { get; set; }
    }
}
