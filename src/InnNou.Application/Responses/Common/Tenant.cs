namespace InnNou.Application.Responses.Common
{
    public class Tenant
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = default!;
        public string Slug { get; set; } = default!;
        public DateTime CreatedUtc { get; set; }
    }
}
