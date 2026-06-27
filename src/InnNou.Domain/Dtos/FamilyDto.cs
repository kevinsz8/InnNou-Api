namespace InnNou.Domain.Dtos
{
    public class FamilyDto
    {
        public int FamilyId { get; set; }
        public Guid FamilyToken { get; set; }
        public string Code { get; set; } = default!;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
    }
}
