namespace InnNou.Application.Responses.Common
{
    public class DepartmentParLevel
    {
        public Guid DepartmentParLevelToken { get; set; }
        public Guid DepartmentToken { get; set; }
        public string? DepartmentName { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public decimal MinimumQuantity { get; set; }
        public decimal ReorderQuantity { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
