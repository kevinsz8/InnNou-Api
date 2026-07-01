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
    }
}
