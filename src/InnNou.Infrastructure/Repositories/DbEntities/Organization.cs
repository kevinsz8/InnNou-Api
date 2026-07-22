namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Organization
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
        public int? ZoneId { get; set; }
        public Guid? ZoneToken { get; set; }
        public string? ZoneCode { get; set; }
        public string? ZoneName { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }

        // Physical address + free-form description — purely informational/reference, no
        // enforcement logic tied to them (unlike ZoneId above, which does gate delivery-zone
        // coverage). Same shape as Suppliers/OrganizationContacts/Warehouses.
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Description { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
        public DateTime? DeletedUtc { get; set; }
        public string? DeletedBy { get; set; }
    }
}
