namespace InnNou.Domain.Dtos
{
    public class InternalOrderShipmentLineDto
    {
        public Guid InternalOrderShipmentLineToken { get; set; }
        public Guid InternalOrderLineToken { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? PurchaseUnitCode { get; set; }

        public decimal QuantityShipped { get; set; }
        public decimal QuantityReceived { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
