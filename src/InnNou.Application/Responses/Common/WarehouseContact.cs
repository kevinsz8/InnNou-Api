namespace InnNou.Application.Responses.Common
{
    public class WarehouseContact
    {
        public int WarehouseContactId { get; set; }
        public Guid WarehouseContactToken { get; set; }
        public int WarehouseId { get; set; }
        public string ContactName { get; set; } = default!;
        public string? ContactType { get; set; }
        public string? Department { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; }
        public bool HasAccessToSystem { get; set; }
        public bool IsActive { get; set; }
    }
}
