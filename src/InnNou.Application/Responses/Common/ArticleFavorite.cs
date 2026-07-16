namespace InnNou.Application.Responses.Common
{
    public class ArticleFavorite
    {
        public Guid ArticleFavoriteToken { get; set; }
        public Guid ArticleToken { get; set; }
        public string ArticleName { get; set; } = default!;
        public string? SupplierSku { get; set; }
        public string? SupplierName { get; set; }
        public Guid OrganizationToken { get; set; }
        public string OrganizationName { get; set; } = default!;
        public bool IsInherited { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
