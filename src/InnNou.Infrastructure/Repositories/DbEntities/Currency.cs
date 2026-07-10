namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Currency
    {
        public int CurrencyId { get; set; }
        public string Code { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}
