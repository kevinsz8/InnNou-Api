namespace InnNou.Domain.Dtos
{
    public class DashboardSummaryDto
    {
        public int PendingApprovalsCount { get; set; }
        public int BelowParCount { get; set; }

        // Null when there's truly nothing to scope spend to (a bare non-SuperAdmin session with
        // no organization). No FX conversion exists anywhere in this codebase, so these are
        // always denominated in a single resolved currency, never blended across currencies.
        public decimal? SpendThisMonth { get; set; }
        public decimal? SpendLastMonth { get; set; }
        public string? SpendCurrencyCode { get; set; }

        public List<MonthlySpendDto> MonthlySpend { get; set; } = [];

        // Dense 7-month x 4-PurchaseOrderStatus grid (28 rows, zero-filled), built in
        // DashboardService from the SP's sparse rows — see OrderStatusMonthCountDto.
        public List<OrderStatusMonthCountDto> OrderCountsByMonth { get; set; } = [];

        public int ActiveUserCount { get; set; }
        public int ActiveOrganizationCount { get; set; }

        public List<RecentActivityItemDto> RecentActivity { get; set; } = [];
    }
}
