namespace InnNou.Domain.Dtos
{
    public class UnitOfMeasureDto
    {
        public int UnitOfMeasureId { get; set; }
        public Guid UnitOfMeasureToken { get; set; }
        public int UnitTypeId { get; set; }
        public string Code { get; set; } = default!;
        public string Symbol { get; set; } = default!;
        public int Decimals { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
    }
}
