namespace InnNou.Application.Responses.Common
{
    public class SubCategory
    {
        public Guid SubCategoryToken { get; set; }
        public int CategoryId { get; set; }
        public string Code { get; set; } = default!;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
        public Guid? OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }
    }
}
