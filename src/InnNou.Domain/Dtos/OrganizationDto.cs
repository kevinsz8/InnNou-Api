namespace InnNou.Domain.Dtos
{
    public class OrganizationDto
    {
        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public string Name { get; set; } = default!;
        public string NormalizedName { get; set; } = default!;
        public string? LegalName { get; set; }
        public string? Code { get; set; }
        public int? ParentOrganizationId { get; set; }
        public int OrganizationTypeId { get; set; }
        public string? OrganizationTypeCode { get; set; }
        public string? TimeZone { get; set; }
        public string? CurrencyCode { get; set; }
        public string? LanguageCode { get; set; }

        // Only ever set for ASSOCIATE-type organizations (app-layer enforced in
        // OrganizationService) — Super Asociado orgs are never zoned. ZoneCode/ZoneName/
        // CountryCode/CountryName are read-only, denormalized on read, never round-tripped
        // on write — same convention as SupplierDto.OrganizationName.
        public int? ZoneId { get; set; }
        public Guid? ZoneToken { get; set; }
        public string? ZoneCode { get; set; }
        public string? ZoneName { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }
}
