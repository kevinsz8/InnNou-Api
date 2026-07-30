namespace InnNou.Application.Responses.Common
{
    public class TaxCategory
    {
        public Guid TaxCategoryToken { get; set; }
        public string Code { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}
