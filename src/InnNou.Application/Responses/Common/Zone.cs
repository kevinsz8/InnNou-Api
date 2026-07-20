namespace InnNou.Application.Responses.Common
{
    public class Zone
    {
        public Guid ZoneToken { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}
