namespace InnNou.Domain.Dtos
{
    public class SubCategoryDto
    {
        public int SubCategoryId { get; set; }
        public Guid SubCategoryToken { get; set; }
        public int CategoryId { get; set; }
        public string Code { get; set; } = default!;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
    }
}
