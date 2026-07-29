namespace InnNou.Application.Responses.Common
{
    public class DashboardSummary
    {
        public int PendingApprovalsCount { get; set; }
        public int BelowParCount { get; set; }
        public decimal? SpendThisMonth { get; set; }
        public decimal? SpendLastMonth { get; set; }
        public string? SpendCurrencyCode { get; set; }
        public List<MonthlySpend> MonthlySpend { get; set; } = [];
        public int OpenPurchaseOrdersAwaitingReceiptCount { get; set; }
        public List<SupplierSpend> TopSuppliersBySpend { get; set; } = [];
        public List<RecentActivityItem> RecentActivity { get; set; } = [];
    }
}
