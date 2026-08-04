namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class InternalOrderReceipt
    {
        public int InternalOrderReceiptId { get; set; }
        public Guid InternalOrderReceiptToken { get; set; }
        public int InternalOrderId { get; set; }
        public Guid InternalOrderToken { get; set; }
        public string InternalOrderNumber { get; set; } = default!;

        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
