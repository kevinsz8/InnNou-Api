namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class SubCategory
    {
        public int SubCategoryId { get; set; }
        public Guid SubCategoryToken { get; set; }
        public int CategoryId { get; set; }
        public string Code { get; set; } = default!;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
