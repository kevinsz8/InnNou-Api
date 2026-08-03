namespace InnNou.Domain.Dtos
{
    public class SubFamilyDto
    {
        public int SubFamilyId { get; set; }
        public Guid SubFamilyToken { get; set; }
        public int FamilyId { get; set; }
        public string Code { get; set; } = default!;
        public Dictionary<string, string>? NameTranslations { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
    }
}
