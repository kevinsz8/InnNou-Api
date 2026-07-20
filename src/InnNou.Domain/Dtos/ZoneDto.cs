namespace InnNou.Domain.Dtos
{
    public class ZoneDto
    {
        public int ZoneId { get; set; }
        public Guid ZoneToken { get; set; }
        public int CountryId { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}
