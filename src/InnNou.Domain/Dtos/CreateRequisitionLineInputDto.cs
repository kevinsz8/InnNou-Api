namespace InnNou.Domain.Dtos
{
    // One line requested as part of a single Requisition create/add-line call. QuantityRequested
    // must be > 0 — it's denominated in UnitToken when provided (must resolve to the article's
    // own PurchaseUnitId or a level in its ArticlePackagingLevels chain — see
    // ArticleUnitConversion), or in the article's PurchaseUnitId directly when UnitToken is null.
    public class CreateRequisitionLineInputDto
    {
        public Guid ArticleToken { get; set; }
        public decimal QuantityRequested { get; set; }
        public Guid? UnitToken { get; set; }
        public string? Notes { get; set; }
    }
}
