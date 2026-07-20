namespace InnNou.Domain.Dtos
{
    public class SubCategoryDto
    {
        public int SubCategoryId { get; set; }
        public Guid SubCategoryToken { get; set; }
        public int CategoryId { get; set; }
        public string Code { get; set; } = default!;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }

        // Read-only, denormalized from the parent Category's own OrganizationId — a
        // SubCategory never sets ownership directly, it always inherits its parent's.
        public int? OrganizationId { get; set; }
        public Guid? OrganizationTokenResult { get; set; }
        public string? OrganizationName { get; set; }
    }
}
