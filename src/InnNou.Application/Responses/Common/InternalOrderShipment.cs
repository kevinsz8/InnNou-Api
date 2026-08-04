namespace InnNou.Application.Responses.Common
{
    public class InternalOrderShipment
    {
        public Guid InternalOrderShipmentToken { get; set; }
        public Guid SourceWarehouseToken { get; set; }
        public string? SourceWarehouseName { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        public List<InternalOrderShipmentLine> Lines { get; set; } = [];
    }
}
