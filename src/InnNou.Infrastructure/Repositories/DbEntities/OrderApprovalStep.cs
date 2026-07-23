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

        // Internal-use only, resolved by sp_OrderApprovalStep_GetByEmailToken — the anonymous
        // single-use email-approval token, distinct from OrderApprovalStepToken (which is used
        // by the authenticated flow). Never mapped onto OrderApprovalStepDto/the wire response,
        // same convention as PurchaseOrder.SupplierEmail/SupplierLanguageCode.
        public Guid? EmailApprovalToken { get; set; }
        public DateTime? EmailApprovalTokenExpiresUtc { get; set; }
        public DateTime? EmailApprovalTokenUsedUtc { get; set; }

        // NULL when this step was created by the original Order.Submit evaluation (unchanged,
        // existing behavior). Set when a PurchaseOrderRectification's own re-evaluation crossed
        // a not-yet-approved threshold level — see .claude/PurchaseOrderRectificationModule.md.
        // OrderService.ApproveStepAndAdvanceAsync/RejectOrderApprovalStepAsync branch on this so
        // a rectification's batch is scoped independently of the Order's own submission-completion
        // logic and never confused with it.
        public int? TriggeringPurchaseOrderRectificationId { get; set; }
    }
}
