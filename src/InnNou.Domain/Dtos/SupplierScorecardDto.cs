namespace InnNou.Domain.Dtos
{
    // Percentages are null when there's nothing to compute them from (no receipt lines in the
    // window at all, or none with Article.LeadTimeDays configured for the OTD/OTIF pair) — never
    // fabricated as 0%, which would misrepresent "no data" as "always late".
    public class SupplierScorecardDto
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
