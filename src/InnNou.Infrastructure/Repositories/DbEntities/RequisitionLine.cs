namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class RequisitionLine
    {
        public int RequisitionLineId { get; set; }
        public Guid RequisitionLineToken { get; set; }
        public int RequisitionId { get; set; }
        public Guid RequisitionToken { get; set; }

        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public int PurchaseUnitId { get; set; }
        public string? PurchaseUnitCode { get; set; }

        public decimal QuantityRequested { get; set; }

        // NULL means "entered directly in PurchaseUnitId" — see ArticleUnitConversion. When set,
        // these record what the user actually typed for accurate re-display; QuantityRequested
        // above always stays the PurchaseUnitId-normalized value every other consumer expects.
        public int? RequestedUnitId { get; set; }
        public string? RequestedUnitCode { get; set; }
        public decimal? RequestedQuantity { get; set; }

        // Only populated by sp_RequisitionLine_GetByRequisitionId — the cumulative sum across
        // every RequisitionIssueLine ever posted against this line. Zero for a freshly created
        // line, and for the plain sp_RequisitionLine_Create/Edit/GetByToken results (which don't
        // compute it — there's nothing to sum yet at those call sites).
        public decimal QuantityIssued { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
