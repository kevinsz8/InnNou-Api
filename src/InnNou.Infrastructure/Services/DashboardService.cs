using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Abstractions;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class DashboardService(IDbConnectionFactory connectionFactory) : IDashboardService
{
    private const int SuperAdminRoleLevel = 100;
    private const int RecentActivityCount = 10;
    private const int TopSuppliersCount = 5;

    private sealed class SpendByMonthRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string CurrencyCode { get; set; } = default!;
        public decimal Total { get; set; }
    }

    private sealed class SupplierSpendRow
    {
        public Guid SupplierToken { get; set; }
        public string SupplierName { get; set; } = default!;
        public string CurrencyCode { get; set; } = default!;
        public decimal Total { get; set; }
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(IRequestContext context, CancellationToken cancellationToken)
    {
        // RoleLevel >= 100 (SuperAdmin) is unrestricted/global — same convention as every other
        // service's own read-scope resolution. Anyone else is scoped to their own organization's
        // hierarchy; a non-SuperAdmin session with no organization at all has nothing to scope to.
        var isUnrestricted = context.RoleLevel >= SuperAdminRoleLevel;
        var rootOrganizationId = isUnrestricted ? (int?)null : context.OrganizationId;

        if (!isUnrestricted && rootOrganizationId is null)
            return new DashboardSummaryDto();

        await using var connection = connectionFactory.CreateConnection();

        var pendingApprovalsCount = await connection.ExecuteScalarAsync<int>(
            "sp_Dashboard_GetPendingApprovalsCount", new { RootOrganizationId = rootOrganizationId }, commandType: CommandType.StoredProcedure);

        var belowParCount = await connection.ExecuteScalarAsync<int>(
            "sp_Dashboard_GetBelowParCount", new { RootOrganizationId = rootOrganizationId }, commandType: CommandType.StoredProcedure);

        var openPurchaseOrdersCount = await connection.ExecuteScalarAsync<int>(
            "sp_Dashboard_GetOpenPurchaseOrdersCount", new { RootOrganizationId = rootOrganizationId }, commandType: CommandType.StoredProcedure);

        var spendRows = (await connection.QueryAsync<SpendByMonthRow>(
            "sp_Dashboard_GetSpendByMonth", new { RootOrganizationId = rootOrganizationId }, commandType: CommandType.StoredProcedure)).ToList();

        var supplierSpendRows = (await connection.QueryAsync<SupplierSpendRow>(
            "sp_Dashboard_GetTopSuppliersBySpend", new { RootOrganizationId = rootOrganizationId }, commandType: CommandType.StoredProcedure)).ToList();

        var recentActivity = (await connection.QueryAsync<RecentActivityItemDto>(
            "sp_Dashboard_GetRecentActivity", new { RootOrganizationId = rootOrganizationId, Count = RecentActivityCount }, commandType: CommandType.StoredProcedure)).ToList();

        // No FX conversion exists anywhere in this codebase — spend can only ever be reported in
        // one currency, never blended. A real organization resolves its own (walking up the
        // hierarchy, same as ArticlePrice's own currency fallback); a bare, unimpersonated
        // SuperAdmin session has no single organization to resolve one from, so the currency with
        // the largest total across the visible window is reported instead, as a best-effort
        // headline figure.
        string? currencyCode;
        if (rootOrganizationId.HasValue)
        {
            var p = new DynamicParameters();
            p.Add("@OrganizationId", rootOrganizationId.Value);
            p.Add("@CurrencyCode", dbType: DbType.String, size: 10, direction: ParameterDirection.Output);
            await connection.ExecuteAsync("sp_Organization_ResolveCurrencyCode", p, commandType: CommandType.StoredProcedure);
            currencyCode = p.Get<string?>("@CurrencyCode");
        }
        else
        {
            currencyCode = spendRows
                .GroupBy(r => r.CurrencyCode)
                .OrderByDescending(g => g.Sum(r => r.Total))
                .Select(g => g.Key)
                .FirstOrDefault();
        }

        var now = DateTime.UtcNow;
        var monthlySpend = Enumerable.Range(0, 7)
            .Select(i => now.AddMonths(-6 + i))
            .Select(d => new MonthlySpendDto
            {
                Year = d.Year,
                Month = d.Month,
                Total = spendRows.Where(r => r.Year == d.Year && r.Month == d.Month && r.CurrencyCode == currencyCode).Sum(r => r.Total)
            })
            .ToList();

        // Same currency filter as monthlySpend above — a supplier billed in a currency
        // other than the resolved headline currency never displaces a same-currency
        // supplier from the top-5 trim.
        var topSuppliersBySpend = supplierSpendRows
            .Where(r => r.CurrencyCode == currencyCode)
            .OrderByDescending(r => r.Total)
            .Take(TopSuppliersCount)
            .Select(r => new SupplierSpendDto
            {
                SupplierToken = r.SupplierToken,
                SupplierName = r.SupplierName,
                Total = r.Total
            })
            .ToList();

        return new DashboardSummaryDto
        {
            PendingApprovalsCount = pendingApprovalsCount,
            BelowParCount = belowParCount,
            SpendThisMonth = monthlySpend[^1].Total,
            SpendLastMonth = monthlySpend[^2].Total,
            SpendCurrencyCode = currencyCode,
            MonthlySpend = monthlySpend,
            OpenPurchaseOrdersAwaitingReceiptCount = openPurchaseOrdersCount,
            TopSuppliersBySpend = topSuppliersBySpend,
            RecentActivity = recentActivity
        };
    }
}
