namespace InnNou.Domain.Dtos
{
    public class SupplierDeliveryZoneDto
    {
        public int SupplierDeliveryZoneId { get; set; }
        public Guid SupplierDeliveryZoneToken { get; set; }
        public int SupplierId { get; set; }
        public Guid SupplierToken { get; set; }
        public string? SupplierName { get; set; }
        public int ZoneId { get; set; }
        public Guid ZoneToken { get; set; }
        public string? ZoneCode { get; set; }
        public string? ZoneName { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
        public int DayOfWeek { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
