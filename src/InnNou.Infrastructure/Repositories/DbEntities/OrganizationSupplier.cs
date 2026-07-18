namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class OrganizationSupplier
    {
        public int OrganizationSupplierId { get; set; }
        public int OrganizationId { get; set; }
        public int SupplierId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }

        // Only populated by sp_OrganizationSupplier_GetActiveBySupplierId (joins Organizations) —
        // left null when populated by sp_OrganizationSupplier_Assign, which doesn't join.
        public Guid? OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }
    }
}
