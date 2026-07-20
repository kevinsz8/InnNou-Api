namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Country
    {
        public int CountryId { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}
