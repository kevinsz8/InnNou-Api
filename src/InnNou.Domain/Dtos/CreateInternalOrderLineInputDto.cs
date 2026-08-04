namespace InnNou.Domain.Dtos
{
    // One line requested as part of a single InternalOrder create call. Quantity must be > 0.
    // UnitPrice/CurrencyCode are never caller-supplied — always resolved server-side from the
    // destination Organization's own ArticlePrice (see CLAUDE.md's "Internal Orders" section).
    public class CreateInternalOrderLineInputDto
    {
        public Guid ArticleToken { get; set; }
        public decimal Quantity { get; set; }
        public string? Notes { get; set; }
    }
}
