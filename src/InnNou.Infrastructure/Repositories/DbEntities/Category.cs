namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Category
    {
        public int CategoryId { get; set; }
        public Guid CategoryToken { get; set; }
        public string Code { get; set; } = default!;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
