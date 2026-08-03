namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Category
    {
        public int CategoryId { get; set; }
        public Guid CategoryToken { get; set; }
        public string Code { get; set; } = default!;

        // Raw JSON text — see Family.cs (same shape) and .claude/CatalogTranslationsModule.md.
        public string? NameTranslations { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
        public int? OrganizationId { get; set; }
        public Guid? OrganizationTokenResult { get; set; }
        public string? OrganizationName { get; set; }
    }
}
