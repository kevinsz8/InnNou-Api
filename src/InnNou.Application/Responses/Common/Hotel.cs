namespace InnNou.Application.Responses.Common
{
    public class Hotel
    {
        public int HotelId { get; set; }
        public Guid HotelToken { get; set; }
        public string Name { get; set; } = default!;
        public int? ParentHotelId { get; set; }
        public string? TimeZone { get; set; }
        public string? CurrencyCode { get; set; }
        public string? LanguageCode { get; set; }
        public bool IsActive { get; set; }
    }
}
