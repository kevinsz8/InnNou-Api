namespace InnNou.Infrastructure.Repositories.DbEntities
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

        // Only populated by sp_WarehouseContact_GetByToken (joins Warehouses.OrganizationId) —
        // used internally by WarehouseContactService for its authorization check; never mapped
        // onto WarehouseContactDto/the wire response.
        public int? WarehouseOrganizationId { get; set; }
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
