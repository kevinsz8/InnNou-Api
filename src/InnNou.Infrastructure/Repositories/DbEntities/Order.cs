using InnNou.Application.Common;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Order
    {
        public int OrderId { get; set; }
        public Guid OrderToken { get; set; }
        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public OrderStatus Status { get; set; }
        public string? Notes { get; set; }
        public DateTime? SubmittedUtc { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }

        // Only populated by sp_Order_GetPaged (a cheap CROSS APPLY COUNT, not per-row app-level
        // N+1); GetByToken/Create/Submit/Cancel leave this at 0 and OrderService overwrites it
        // from the real hydrated Lines.Count instead.
        public int LineCount { get; set; }
    }
}
