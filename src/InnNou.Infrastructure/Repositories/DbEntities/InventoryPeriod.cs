using InnNou.Application.Common;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class InventoryPeriod
    {
        public int InventoryPeriodId { get; set; }
        public Guid InventoryPeriodToken { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public int OrganizationId { get; set; }
        public InventoryPeriodStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? ClosedUtc { get; set; }
        public string? ClosedBy { get; set; }
        public DateTime? ReopenedUtc { get; set; }
        public string? ReopenedBy { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }

        // Only populated by sp_InventoryPeriod_GetPaged (a cheap CROSS APPLY COUNT, same
        // convention as GoodsReceipt.LineCount); GetByToken/Create/SetStatus leave this at 0 and
        // InventoryPeriodService overwrites it from the real hydrated Lines.Count instead.
        public int LineCount { get; set; }
    }
}
