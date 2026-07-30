namespace InnNou.Application.Responses.Common
{
    public class SupplierScorecard
    {
        public Guid SupplierToken { get; set; }
        public int TotalReceiptLines { get; set; }
        public decimal TotalAccepted { get; set; }
        public decimal TotalCourtesy { get; set; }
        public decimal TotalRejected { get; set; }
        public decimal? RejectionRatePercent { get; set; }
        public int OtdEligibleLines { get; set; }
        public decimal? OnTimeDeliveryPercent { get; set; }
        public decimal? OnTimeInFullPercent { get; set; }
        public decimal? AvgLeadTimeDays { get; set; }
    }
}
