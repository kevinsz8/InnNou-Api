namespace InnNou.Application.Responses.Common
{
    public class UnitOfMeasure
    {
        public Guid UnitOfMeasureToken { get; set; }
        public int UnitTypeId { get; set; }
        public string Code { get; set; } = default!;
        public string Symbol { get; set; } = default!;
        public int Decimals { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
    }
}
