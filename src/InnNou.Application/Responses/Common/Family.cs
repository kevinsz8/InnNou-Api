namespace InnNou.Application.Responses.Common
{
    public class Family
    {
        public Guid FamilyToken { get; set; }
        public string Code { get; set; } = default!;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
    }
}
