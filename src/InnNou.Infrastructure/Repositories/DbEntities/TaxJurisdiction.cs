namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class TaxJurisdiction
    {
        public int TaxJurisdictionId { get; set; }
        public Guid TaxJurisdictionToken { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; }
        public int CountryId { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
    }
}
