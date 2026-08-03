namespace InnNou.Application.Responses.Common
{
    public class Family
    {
        public Guid FamilyToken { get; set; }
        public string Code { get; set; } = default!;
        public Dictionary<string, string>? NameTranslations { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
        public Guid? DefaultTaxCategoryToken { get; set; }
        public string? DefaultTaxCategoryCode { get; set; }
    }
}
