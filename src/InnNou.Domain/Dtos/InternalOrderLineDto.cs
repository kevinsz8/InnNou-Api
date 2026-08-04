namespace InnNou.Domain.Dtos
{
    public class InternalOrderLineDto
    {
        public Guid InternalOrderLineToken { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? SupplierSku { get; set; }

        public decimal Quantity { get; set; }
        public string? PurchaseUnitCode { get; set; }

        // The destination Organization's own resolved ArticlePrice, frozen at request time.
        public decimal UnitPrice { get; set; }
        public string CurrencyCode { get; set; } = default!;

        public string? Notes { get; set; }

        // Populated by InternalOrderService.GetByTokenAsync via
        // sp_InternalOrderLine_GetByInternalOrderId — lets the UI show remaining-to-ship/
        // remaining-to-receive without a second round trip.
        public decimal QuantityShipped { get; set; }
        public decimal QuantityAccepted { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
