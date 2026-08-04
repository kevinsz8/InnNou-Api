namespace InnNou.Application.Responses.Common
{
    public class InternalOrderReceipt
    {
        public Guid InternalOrderReceiptToken { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        public List<InternalOrderReceiptLine> Lines { get; set; } = [];
    }
}
