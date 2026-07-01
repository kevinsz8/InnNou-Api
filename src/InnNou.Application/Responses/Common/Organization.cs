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
        public bool IsActive { get; set; }
    }
}
