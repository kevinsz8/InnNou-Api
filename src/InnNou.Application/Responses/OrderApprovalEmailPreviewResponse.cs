namespace InnNou.Application.Responses
{
    public class OrderApprovalEmailPreviewResponse
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
