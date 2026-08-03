namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Family
    {
        public int FamilyId { get; set; }
        public Guid FamilyToken { get; set; }
        public string Code { get; set; } = default!;

        // Raw JSON text, e.g. '{"es":"Bebidas","en":"Beverages","ca":"Begudes"}' — parsed into a
        // Dictionary<string,string> at the mapping layer (see InfrastructureMappings.cs). See
        // .claude/CatalogTranslationsModule.md.
        public string? NameTranslations { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
        public int? DefaultTaxCategoryId { get; set; }
        public Guid? DefaultTaxCategoryToken { get; set; }
        public string? DefaultTaxCategoryCode { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
