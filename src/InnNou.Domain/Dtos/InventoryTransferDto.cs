namespace InnNou.Domain.Dtos
{
    public class InventoryTransferDto
    {
        public Guid InventoryTransferToken { get; set; }
        public Guid FromWarehouseToken { get; set; }
        public string? FromWarehouseName { get; set; }
        public Guid ToWarehouseToken { get; set; }
        public string? ToWarehouseName { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        public int LineCount { get; set; }

        // Populated by InventoryService via sp_InventoryTransferLine_GetByTransferId.
        public List<InventoryTransferLineDto> Lines { get; set; } = [];
    }
}
