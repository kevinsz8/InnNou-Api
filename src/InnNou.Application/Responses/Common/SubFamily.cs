namespace InnNou.Application.Responses.Common
{
    public class SubFamily
    {
        public Guid SubFamilyToken { get; set; }
        public int FamilyId { get; set; }
        public string Code { get; set; } = default!;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
    }
}
