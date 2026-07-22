namespace InnNou.Domain.Dtos
{
    public class SupplierDto
    {
        public int SupplierId { get; set; }
        public Guid SupplierToken { get; set; }
        public string Name { get; set; } = default!;
        public string? LegalName { get; set; }
        public string? TaxId { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? LanguageCode { get; set; }
        public bool? IsGlobal { get; set; }
        public string? SupplierType { get; set; }

        // Read-only, denormalized — the actual image file lives on local disk (see CLAUDE.md's
        // "Supplier logo" note), set only via UploadLogoAsync/DeleteLogoAsync, never accepted as
        // input on Create/Edit.
        public string? LogoUrl { get; set; }
        public bool? HasAccessToSystem { get; set; }
        public string? LoginEmail { get; set; }
        public string? Password { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        // Transient, write-only inputs — never round-tripped in a response mapping, same
        // pattern as LoginEmail/Password above. OrganizationToken names the target owner when
        // IsGlobal is false (create), or reassigns/keeps ownership (edit) — see
        // SupplierService.CreateSupplierAsync/EditSupplierAsync. ConfirmPrivatizationImpact is
        // the resubmit-with-confirm flag for a SuperAdmin-initiated Global->Private or
        // owner-reassignment edit that would remove another organization's existing access.
        public Guid? OrganizationToken { get; set; }
        public bool ConfirmPrivatizationImpact { get; set; }

        // Read-only outputs, resolved by the service (not the entity mapper — Suppliers itself
        // has no owner column, ownership lives in OrganizationSuppliers) and stitched onto the
        // DTO for display. Null on both = global supplier.
        public Guid? OrganizationTokenResult { get; set; }
        public string? OrganizationName { get; set; }
    }
}
