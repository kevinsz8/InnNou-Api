namespace InnNou.Application.Responses
{
    public class CreateSupplierCommandResponse
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
        public bool IsGlobal { get; set; }
        public string SupplierType { get; set; } = default!;
        public bool HasAccessToSystem { get; set; }
        public bool IsActive { get; set; }
        public Guid? OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }
    }
}
