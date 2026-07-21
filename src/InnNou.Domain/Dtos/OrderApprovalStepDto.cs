namespace InnNou.Domain.Dtos
{
    public class OrderApprovalStepDto
    {
        public int OrderApprovalStepId { get; set; }
        public Guid OrderApprovalStepToken { get; set; }
        public int OrderId { get; set; }
        public Guid OrderToken { get; set; }

        // Only populated by sp_OrderApprovalStep_GetPendingForApprover (the "pending my
        // approval" list needs to show which Order/Organization/Warehouse it belongs to
        // without a second round trip); null from sp_OrderApprovalStep_GetByOrderId, where the
        // caller already has the parent OrderDto.
        public Guid? OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }
        public Guid? WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }

        public int FamilyId { get; set; }
        public string FamilyCode { get; set; } = default!;
        public int Level { get; set; }
        public decimal ThresholdAmount { get; set; }
        public decimal ActualFamilyAmount { get; set; }
        public string CurrencyCode { get; set; } = default!;
        public int ApproverUserId { get; set; }
        public Guid ApproverUserToken { get; set; }
        public string ApproverName { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime? DecidedUtc { get; set; }
        public string? DecidedBy { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
