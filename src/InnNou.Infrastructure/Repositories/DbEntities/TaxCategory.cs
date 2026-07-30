namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class TaxCategory
    {
        public int TaxCategoryId { get; set; }
        public Guid TaxCategoryToken { get; set; }
        public string Code { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}
