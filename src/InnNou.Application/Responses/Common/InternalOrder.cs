namespace InnNou.Application.Responses.Common
{
    public class InternalOrder
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

        public List<InternalOrderLine> Lines { get; set; } = [];
        public List<InternalOrderShipment> Shipments { get; set; } = [];
        public List<InternalOrderReceipt> Receipts { get; set; } = [];
    }
}
