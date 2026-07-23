namespace InnNou.Application.Responses.Common
{
    public class PurchaseOrderRectification
    {
        public Guid PurchaseOrderRectificationToken { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public int SequenceNumber { get; set; }
        public string Reason { get; set; } = default!;
        public string? Notes { get; set; }
        public string Status { get; set; } = default!;
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? AppliedUtc { get; set; }
        public List<PurchaseOrderLineRectification> Lines { get; set; } = [];
    }
}
