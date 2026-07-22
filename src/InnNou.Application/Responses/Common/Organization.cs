namespace InnNou.Application.Responses.Common
{
    public class Organization
    {
        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public string Name { get; set; } = default!;
        public int? ParentOrganizationId { get; set; }
        public int OrganizationTypeId { get; set; }
        public string? OrganizationTypeCode { get; set; }
        public string? TimeZone { get; set; }
        public string? CurrencyCode { get; set; }
        public string? LanguageCode { get; set; }
        public Guid? ZoneToken { get; set; }
        public string? ZoneCode { get; set; }
        public string? ZoneName { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
