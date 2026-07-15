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
        public string SupplierType { get; set; } = default!;
        public bool HasAccessToSystem { get; set; }
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
