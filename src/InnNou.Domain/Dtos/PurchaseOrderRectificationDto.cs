namespace InnNou.Domain.Dtos
{
    public class PurchaseOrderRectificationDto
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

        // Populated by PurchaseOrderService via sp_PurchaseOrderLineRectification_GetByRectificationId
        // — the individual line-level deltas that make up this rectification event.
        public List<PurchaseOrderLineRectificationDto> Lines { get; set; } = [];
    }
}
