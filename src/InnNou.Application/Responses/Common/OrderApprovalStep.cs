namespace InnNou.Application.Responses.Common
{
    public class OrderApprovalStep
    {
        public Guid OrderApprovalStepToken { get; set; }
        public Guid OrderToken { get; set; }
        public Guid? OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }
        public Guid? WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public string FamilyCode { get; set; } = default!;
        public int Level { get; set; }
        public decimal ThresholdAmount { get; set; }
        public decimal ActualFamilyAmount { get; set; }
        public string CurrencyCode { get; set; } = default!;
        public Guid ApproverUserToken { get; set; }
        public string ApproverName { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime? DecidedUtc { get; set; }
        public string? DecidedBy { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
