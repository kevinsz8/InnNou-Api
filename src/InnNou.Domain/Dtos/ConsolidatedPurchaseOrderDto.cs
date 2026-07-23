namespace InnNou.Domain.Dtos
{
    public class ConsolidatedPurchaseOrderDto
    {
        public Guid ConsolidatedPurchaseOrderToken { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public Guid SuperAssociateOrganizationToken { get; set; }
        public string? SuperAssociateOrganizationName { get; set; }
        public string? Title { get; set; }
        public string? Notes { get; set; }
        public DateTime DateRangeFrom { get; set; }
        public DateTime DateRangeTo { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public int MemberCount { get; set; }

        // Populated by ConsolidatedPurchaseOrderService via
        // sp_ConsolidatedPurchaseOrderMember_GetByConsolidatedPurchaseOrderId — the individual
        // PurchaseOrders (from various properties) pulled into this negotiation snapshot.
        public List<ConsolidatedPurchaseOrderMemberDto> Members { get; set; } = [];
    }
}
