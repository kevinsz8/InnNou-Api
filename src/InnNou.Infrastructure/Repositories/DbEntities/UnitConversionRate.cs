namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class UnitConversionRate
    {
        public int UnitConversionRateId { get; set; }
        public Guid UnitConversionRateToken { get; set; }
        public int FromUnitOfMeasureId { get; set; }
        public int ToUnitOfMeasureId { get; set; }
        public decimal Factor { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
