namespace InnNou.Application.Responses.Common
{
    public class SupplierDeliveryZone
    {
        public Guid SupplierDeliveryZoneToken { get; set; }
        public Guid SupplierToken { get; set; }
        public string? SupplierName { get; set; }
        public Guid ZoneToken { get; set; }
        public string? ZoneCode { get; set; }
        public string? ZoneName { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
        public int DayOfWeek { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
