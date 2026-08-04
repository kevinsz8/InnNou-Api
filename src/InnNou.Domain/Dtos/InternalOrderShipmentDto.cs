namespace InnNou.Domain.Dtos
{
    public class InternalOrderShipmentDto
    {
        public Guid InternalOrderShipmentToken { get; set; }
        public Guid SourceWarehouseToken { get; set; }
        public string? SourceWarehouseName { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        // Populated by InternalOrderService via sp_InternalOrderShipmentLine_GetByInternalOrderShipmentId.
        public List<InternalOrderShipmentLineDto> Lines { get; set; } = [];
    }
}
