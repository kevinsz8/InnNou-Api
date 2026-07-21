namespace InnNou.Application.Responses.Common
{
    public class ArticleClassification
    {
        public Guid ArticleClassificationToken { get; set; }
        public Guid ArticleToken { get; set; }
        public string ArticleName { get; set; } = default!;
        public string? SupplierSku { get; set; }
        public string? SupplierName { get; set; }
        public Guid OrganizationToken { get; set; }
        public string OrganizationName { get; set; } = default!;
        public Guid CategoryToken { get; set; }
        public string CategoryCode { get; set; } = default!;
        public Guid? SubCategoryToken { get; set; }
        public string? SubCategoryCode { get; set; }
        public bool IsInherited { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
