namespace InnNou.Domain.Dtos
{
    public class WarehouseContactDto
    {
        public int WarehouseContactId { get; set; }
        public Guid WarehouseContactToken { get; set; }

        // Write-only bridge field: resolved to WarehouseId inside WarehouseContactService,
        // same pattern as OrganizationContactDto.OrganizationToken — never populated on reads.
        public Guid WarehouseToken { get; set; }
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

        // HasAccessToSystem is nullable so an edit can distinguish "not supplied" from
        // "false" (same reasoning as SupplierDto). LoginEmail/Password are transient,
        // write-only fields that flow into WarehouseContactService to create/update the
        // linked shadow User — they never round-trip in a response mapping.
        public bool? HasAccessToSystem { get; set; }
        public string? LoginEmail { get; set; }
        public string? Password { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }
}
