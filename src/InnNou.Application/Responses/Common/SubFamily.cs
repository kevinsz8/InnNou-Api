namespace InnNou.Application.Responses.Common
{
    public class SubFamily
    {
        public Guid SubFamilyToken { get; set; }
        public int FamilyId { get; set; }
        public string Code { get; set; } = default!;
        public Dictionary<string, string>? NameTranslations { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
    }
}
