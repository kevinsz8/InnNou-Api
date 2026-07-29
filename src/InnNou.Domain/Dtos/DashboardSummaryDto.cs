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

        // SENT + PARTIALLY_RECEIVED PurchaseOrder count — "what's in transit right now",
        // a current-state snapshot rather than a month-bucketed history.
        public int OpenPurchaseOrdersAwaitingReceiptCount { get; set; }

        // Top 5 suppliers by spend this calendar month, already filtered to
        // SpendCurrencyCode and trimmed in DashboardService — see SupplierSpendDto.
        public List<SupplierSpendDto> TopSuppliersBySpend { get; set; } = [];

        public List<RecentActivityItemDto> RecentActivity { get; set; } = [];
    }
}
