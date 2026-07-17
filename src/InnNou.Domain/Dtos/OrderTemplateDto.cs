namespace InnNou.Domain.Dtos
{
    public class OrderTemplateDto
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
        public int LineCount { get; set; }

        // Populated by OrderTemplateService after a second sp_OrderTemplateLine_GetByOrderTemplateId
        // call — never resolved by the mapper directly, same reasoning OrderDto.Lines requires.
        public List<OrderTemplateLineDto> Lines { get; set; } = [];
    }
}
