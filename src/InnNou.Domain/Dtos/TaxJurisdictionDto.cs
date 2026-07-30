namespace InnNou.Domain.Dtos
{
    public class TaxJurisdictionDto
    {
        public Guid TaxJurisdictionToken { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
    }
}
