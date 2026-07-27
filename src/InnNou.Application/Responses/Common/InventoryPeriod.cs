namespace InnNou.Application.Responses.Common
{
    public class InventoryPeriod
    {
        public Guid InventoryPeriodToken { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public string Status { get; set; } = default!;
        public DateTime StartDate { get; set; }
        public DateTime? ClosedUtc { get; set; }
        public string? ClosedBy { get; set; }
        public DateTime? ReopenedUtc { get; set; }
        public string? ReopenedBy { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public int LineCount { get; set; }
        public List<InventoryPeriodCount> Lines { get; set; } = [];
    }
}
