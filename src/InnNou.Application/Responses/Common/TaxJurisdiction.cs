namespace InnNou.Application.Responses.Common
{
    public class TaxJurisdiction
    {
        public Guid TaxJurisdictionToken { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
    }
}
