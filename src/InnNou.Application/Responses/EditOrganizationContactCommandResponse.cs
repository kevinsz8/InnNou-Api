namespace InnNou.Application.Responses
{
    public class EditOrganizationContactCommandResponse
    {
        public int OrganizationContactId { get; set; }
        public Guid OrganizationContactToken { get; set; }
        public int OrganizationId { get; set; }
        public string ContactName { get; set; } = default!;
        public string? ContactType { get; set; }
        public string? Department { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
    }
}
