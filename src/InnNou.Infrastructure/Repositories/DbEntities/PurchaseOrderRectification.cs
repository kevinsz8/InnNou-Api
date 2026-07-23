using InnNou.Application.Common;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class PurchaseOrderRectification
    {
        public int PurchaseOrderRectificationId { get; set; }
        public Guid PurchaseOrderRectificationToken { get; set; }
        public int PurchaseOrderId { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public int SequenceNumber { get; set; }
        public string Reason { get; set; } = default!;
        public string? Notes { get; set; }
        public PurchaseOrderRectificationStatus Status { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? AppliedUtc { get; set; }
    }
}
