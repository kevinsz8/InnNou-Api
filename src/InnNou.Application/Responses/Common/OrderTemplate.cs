namespace InnNou.Application.Responses.Common
{
    public class OrderTemplate
    {
        public Guid OrderTemplateToken { get; set; }
        public Guid OrganizationToken { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public bool IsWarehouseActive { get; set; }
        public Guid OwnerUserToken { get; set; }
        public string? OwnerFirstName { get; set; }
        public string? OwnerLastName { get; set; }
        public string? OwnerEmail { get; set; }
        public string Name { get; set; } = default!;
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
        public int LineCount { get; set; }
        public List<OrderTemplateLine> Lines { get; set; } = [];
    }
}
