namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class InternalOrderShipmentLine
    {
        public int InternalOrderShipmentLineId { get; set; }
        public Guid InternalOrderShipmentLineToken { get; set; }
        public int InternalOrderShipmentId { get; set; }
        public Guid InternalOrderShipmentToken { get; set; }

        public int InternalOrderLineId { get; set; }
        public Guid InternalOrderLineToken { get; set; }

        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? PurchaseUnitCode { get; set; }

        public decimal QuantityShipped { get; set; }

        // Populated only by sp_InternalOrderShipmentLine_GetByInternalOrderShipmentId (combined)
        // or sp_InternalOrderShipmentLine_GetByInternalOrderId (split) — never both at once.
        public decimal QuantityReceived { get; set; }
        public decimal QuantityAccepted { get; set; }
        public decimal QuantityRejected { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
