namespace InnNou.Domain.Dtos
{
    public class CurrencyDto
    {
        public int CurrencyId { get; set; }
        public string Code { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}
