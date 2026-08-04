namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class InternalOrderLine
    {
        public int InternalOrderLineId { get; set; }
        public Guid InternalOrderLineToken { get; set; }
        public int InternalOrderId { get; set; }
        public Guid InternalOrderToken { get; set; }

        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? SupplierSku { get; set; }

        public decimal Quantity { get; set; }
        public string? PurchaseUnitCode { get; set; }

        public decimal UnitPrice { get; set; }
        public string CurrencyCode { get; set; } = default!;

        public string? Notes { get; set; }

        // Only populated by sp_InternalOrderLine_GetByInternalOrderId (not by the plain Create
        // re-select, which has nothing shipped/received yet).
        public decimal QuantityShipped { get; set; }
        public decimal QuantityAccepted { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
