namespace InnNou.Domain.Dtos
{
    // Read-only preview shown on the anonymous /approve-order/:token confirmation page before
    // the human clicks Approve. Status ("Ready"/"Expired"/"AlreadyUsed"/"AlreadyDecided") is a
    // normal, informative outcome for a one-click link — not modeled as a failure response the
    // way the mutating approve call is (see OrderApprovalEmailApproveResultDto).
    public class OrderApprovalEmailPreviewDto
    {
        public string Status { get; set; } = default!;
        public string OrganizationName { get; set; } = default!;
        public string WarehouseName { get; set; } = default!;
        public string FamilyCode { get; set; } = default!;
        public int Level { get; set; }
        public decimal ThresholdAmount { get; set; }
        public decimal ActualFamilyAmount { get; set; }
        public string CurrencyCode { get; set; } = default!;
        public string OrderReference { get; set; } = default!;
    }
}
