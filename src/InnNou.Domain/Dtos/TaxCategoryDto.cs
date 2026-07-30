namespace InnNou.Domain.Dtos
{
    public class TaxCategoryDto
    {
        public Guid TaxCategoryToken { get; set; }
        public string Code { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}
