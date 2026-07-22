using InnNou.Application.Common;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class OrderApprovalStep
    {
        public int OrderApprovalStepId { get; set; }
        public Guid OrderApprovalStepToken { get; set; }
        public int OrderId { get; set; }
        public Guid OrderToken { get; set; }
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
        public OrderApprovalStepStatus Status { get; set; }
        public DateTime? DecidedUtc { get; set; }
        public string? DecidedBy { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
