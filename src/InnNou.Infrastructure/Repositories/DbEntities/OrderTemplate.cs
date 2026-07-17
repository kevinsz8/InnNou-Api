namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class OrderTemplate
    {
        public int OrderTemplateId { get; set; }
        public Guid OrderTemplateToken { get; set; }
        public string Name { get; set; } = default!;
        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public bool IsWarehouseActive { get; set; }
        public int OwnerUserId { get; set; }
        public Guid OwnerUserToken { get; set; }
        public string? OwnerFirstName { get; set; }
        public string? OwnerLastName { get; set; }
        public string? OwnerEmail { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }

        // Only populated by sp_OrderTemplate_GetPaged/GetByToken/Rename (a cheap CROSS APPLY
        // COUNT); Create leaves this at 0 (a fresh template always starts empty).
        public int LineCount { get; set; }
    }
}
