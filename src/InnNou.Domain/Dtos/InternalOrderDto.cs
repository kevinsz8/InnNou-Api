namespace InnNou.Domain.Dtos
{
    public class InternalOrderDto
    {
        public Guid InternalOrderToken { get; set; }
        public string InternalOrderNumber { get; set; } = default!;

        public Guid RequestingOrganizationToken { get; set; }
        public string? RequestingOrganizationName { get; set; }

        public Guid SourceOrganizationToken { get; set; }
        public string? SourceOrganizationName { get; set; }

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

        public int LineCount { get; set; }

        // Populated by InternalOrderService.GetByTokenAsync only — GetPagedAsync leaves these
        // empty and relies on LineCount, same "header list vs. full detail" split as GoodsReceipt/
        // InventoryTransfer.
        public List<InternalOrderLineDto> Lines { get; set; } = [];
        public List<InternalOrderShipmentDto> Shipments { get; set; } = [];
        public List<InternalOrderReceiptDto> Receipts { get; set; } = [];
    }
}
