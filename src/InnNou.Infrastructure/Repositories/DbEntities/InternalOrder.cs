namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class InternalOrder
    {
        public int InternalOrderId { get; set; }
        public Guid InternalOrderToken { get; set; }
        public string InternalOrderNumber { get; set; } = default!;

        public int RequestingOrganizationId { get; set; }
        public Guid RequestingOrganizationToken { get; set; }
        public string? RequestingOrganizationName { get; set; }

        public int SourceOrganizationId { get; set; }
        public Guid SourceOrganizationToken { get; set; }
        public string? SourceOrganizationName { get; set; }

        public int DestinationWarehouseId { get; set; }
        public Guid DestinationWarehouseToken { get; set; }
        public string? DestinationWarehouseName { get; set; }

        public string Status { get; set; } = default!;
        public string? Notes { get; set; }

        public DateTime? CancelledUtc { get; set; }
        public string? CancelledBy { get; set; }
        public string? CancelledReason { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }

        // Only populated by sp_InternalOrder_GetPaged (a cheap CROSS APPLY COUNT, same convention
        // as PurchaseOrder/InventoryTransfer's own LineCount); InternalOrderService overwrites it
        // from the real hydrated Lines.Count wherever Lines is populated instead.
        public int LineCount { get; set; }
    }
}
