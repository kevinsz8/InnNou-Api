namespace InnNou.Domain.Dtos
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public Guid CategoryToken { get; set; }
        public string Code { get; set; } = default!;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
    }
}
