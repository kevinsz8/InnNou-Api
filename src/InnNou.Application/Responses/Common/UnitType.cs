namespace InnNou.Application.Responses.Common
{
    public class UnitType
    {
        public Guid UnitTypeToken { get; set; }
        public string Code { get; set; } = default!;
        public Dictionary<string, string>? NameTranslations { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
    }
}
