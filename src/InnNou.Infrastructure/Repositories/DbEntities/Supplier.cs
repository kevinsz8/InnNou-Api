using InnNou.Application.Common;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Supplier
    {
        public int SupplierId { get; set; }
        public Guid SupplierToken { get; set; }
        public string Name { get; set; } = default!;
        public string NormalizedName { get; set; } = default!;
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
        public bool IsGlobal { get; set; }
        public SupplierType SupplierType { get; set; }
        public string? LogoUrl { get; set; }
        public bool HasAccessToSystem { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        // Populated only by SPs that join OrganizationSuppliers (sp_Supplier_GetPaged,
        // sp_Supplier_GetByToken) — null on both for a global supplier or when the issuing SP
        // doesn't select them (Dapper leaves unmatched properties at their default).
        public Guid? OrganizationTokenResult { get; set; }
        public string? OrganizationName { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
        public DateTime? DeletedUtc { get; set; }
        public string? DeletedBy { get; set; }
    }
}
