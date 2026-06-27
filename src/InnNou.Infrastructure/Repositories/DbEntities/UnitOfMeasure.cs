namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class UnitOfMeasure
    {
        public int UnitOfMeasureId { get; set; }
        public Guid UnitOfMeasureToken { get; set; }
        public int UnitTypeId { get; set; }
        public string Code { get; set; } = default!;
        public string Symbol { get; set; } = default!;
        public int Decimals { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
