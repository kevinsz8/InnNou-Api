namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class InventoryTransfer
    {
        public int InventoryTransferId { get; set; }
        public Guid InventoryTransferToken { get; set; }
        public int FromWarehouseId { get; set; }
        public Guid FromWarehouseToken { get; set; }
        public string? FromWarehouseName { get; set; }

        // Only populated by sp_InventoryTransfer_GetByToken — used to enforce the
        // same-Organization rule without a second Warehouse lookup.
        public int? FromOrganizationId { get; set; }

        public int ToWarehouseId { get; set; }
        public Guid ToWarehouseToken { get; set; }
        public string? ToWarehouseName { get; set; }
        public int? ToOrganizationId { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        // Only populated by sp_InventoryTransfer_GetPaged (a cheap CROSS APPLY COUNT, same
        // convention as PurchaseOrder.LineCount); InventoryService overwrites it from the real
        // hydrated Lines.Count wherever Lines is populated instead.
        public int LineCount { get; set; }
    }
}
