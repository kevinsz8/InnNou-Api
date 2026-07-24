namespace InnNou.Application.Responses.Common
{
    public class InventoryTransfer
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
        public List<InventoryTransferLine> Lines { get; set; } = [];
    }
}
