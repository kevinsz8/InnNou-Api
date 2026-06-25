namespace InnNou.Application.Responses
{
    public class EditHotelCommandResponse
    {
        public int HotelId { get; set; }
        public Guid HotelToken { get; set; }
        public string Name { get; set; } = default!;
        public string? LegalName { get; set; }
        public string? Code { get; set; }
        public int? ParentHotelId { get; set; }
        public string? TimeZone { get; set; }
        public string? CurrencyCode { get; set; }
        public string? LanguageCode { get; set; }
        public bool IsActive { get; set; }
    }
}
