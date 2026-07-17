namespace InnNou.Domain.Dtos
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public Guid OrderToken { get; set; }
        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public string Status { get; set; } = default!;
        public string? Notes { get; set; }
        public DateTime? SubmittedUtc { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }

        // Populated by OrderService after a second sp_OrderLine_GetByOrderId call — never
        // resolved by the mapper directly, same reasoning ArticleDto's IsFavorite required a
        // dedicated query rather than a lazy nav property.
        public List<OrderLineDto> Lines { get; set; } = [];
    }
}
