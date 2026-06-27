namespace InnNou.Domain.Dtos
{
    public class UnitTypeDto
    {
        public int UnitTypeId { get; set; }
        public Guid UnitTypeToken { get; set; }
        public string Code { get; set; } = default!;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
    }
}
