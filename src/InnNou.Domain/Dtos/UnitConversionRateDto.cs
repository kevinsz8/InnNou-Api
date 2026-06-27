namespace InnNou.Domain.Dtos
{
    public class UnitConversionRateDto
    {
        public int UnitConversionRateId { get; set; }
        public Guid UnitConversionRateToken { get; set; }
        public int FromUnitOfMeasureId { get; set; }
        public int ToUnitOfMeasureId { get; set; }
        public decimal Factor { get; set; }
        public bool IsActive { get; set; }
    }
}
