namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class DepartmentParLevel
    {
        public int DepartmentParLevelId { get; set; }
        public Guid DepartmentParLevelToken { get; set; }
        public int DepartmentId { get; set; }
        public Guid DepartmentToken { get; set; }
        public string? DepartmentName { get; set; }
        public int OrganizationId { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }

        // Denominated in Article.PurchaseUnitId.
        public decimal MinimumQuantity { get; set; }
        public decimal ReorderQuantity { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
