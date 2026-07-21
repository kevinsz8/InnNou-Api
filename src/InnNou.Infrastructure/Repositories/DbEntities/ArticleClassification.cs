namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class ArticleClassification
    {
        public int ArticleClassificationId { get; set; }
        public Guid ArticleClassificationToken { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string ArticleName { get; set; } = default!;
        public string? SupplierSku { get; set; }
        public string? SupplierName { get; set; }
        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public string OrganizationName { get; set; } = default!;
        public int CategoryId { get; set; }
        public Guid CategoryToken { get; set; }
        public string CategoryCode { get; set; } = default!;
        public int? SubCategoryId { get; set; }
        public Guid? SubCategoryToken { get; set; }
        public string? SubCategoryCode { get; set; }
        public bool IsInherited { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
