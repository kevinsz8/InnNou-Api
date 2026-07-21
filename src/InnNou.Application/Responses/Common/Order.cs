namespace InnNou.Application.Responses.Common
{
    public class Order
    {
        public Guid OrderToken { get; set; }
        public Guid OrganizationToken { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public string Status { get; set; } = default!;
        public string? Notes { get; set; }
        public DateTime? SubmittedUtc { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
        public int LineCount { get; set; }
        public List<OrderLine> Lines { get; set; } = [];
        public List<OrderApprovalStep> ApprovalSteps { get; set; } = [];
    }
}
