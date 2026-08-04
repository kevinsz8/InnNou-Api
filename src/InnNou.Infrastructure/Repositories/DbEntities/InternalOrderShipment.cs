namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class InternalOrderShipment
    {
        public int InternalOrderShipmentId { get; set; }
        public Guid InternalOrderShipmentToken { get; set; }
        public int InternalOrderId { get; set; }
        public Guid InternalOrderToken { get; set; }
        public string InternalOrderNumber { get; set; } = default!;

        public int SourceWarehouseId { get; set; }
        public Guid SourceWarehouseToken { get; set; }
        public string? SourceWarehouseName { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
