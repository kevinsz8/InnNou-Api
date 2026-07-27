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

    // Every PurchaseOrderStatus code the grid always reports a (possibly zero) row for, so the
    // frontend's toggle buttons never have to special-case a status with no data in the window.
    private static readonly string[] AllPurchaseOrderStatusCodes =
    [
        PurchaseOrderStatusCodes.Sent,
        PurchaseOrderStatusCodes.PartiallyReceived,
        PurchaseOrderStatusCodes.Received,
        PurchaseOrderStatusCodes.Cancelled
    ];

    private sealed class ActiveUserSummaryRow
    {
        public int ActiveUserCount { get; set; }
        public int ActiveOrganizationCount { get; set; }
    }

    private sealed class SpendByMonthRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string CurrencyCode { get; set; } = default!;
        public decimal Total { get; set; }
    }

    private sealed class OrderCountByMonthByStatusRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string StatusCode { get; set; } = default!;
        public int OrderCount { get; set; }
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

        var activeUserSummary = await connection.QueryFirstOrDefaultAsync<ActiveUserSummaryRow>(
            "sp_Dashboard_GetActiveUserSummary", new { RootOrganizationId = rootOrganizationId }, commandType: CommandType.StoredProcedure);

        var spendRows = (await connection.QueryAsync<SpendByMonthRow>(
            "sp_Dashboard_GetSpendByMonth", new { RootOrganizationId = rootOrganizationId }, commandType: CommandType.StoredProcedure)).ToList();

        var orderCountRows = (await connection.QueryAsync<OrderCountByMonthByStatusRow>(
            "sp_Dashboard_GetOrderCountByMonthByStatus", new { RootOrganizationId = rootOrganizationId }, commandType: CommandType.StoredProcedure)).ToList();

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

        // Dense 7-month x 4-status grid, zero-filled — same sparse-rows-in/dense-grid-out shape
        // as monthlySpend above, so the frontend never needs to handle a missing combination.
        var orderCountsByMonth = Enumerable.Range(0, 7)
            .Select(i => now.AddMonths(-6 + i))
            .SelectMany(d => AllPurchaseOrderStatusCodes.Select(statusCode => new OrderStatusMonthCountDto
            {
                Year = d.Year,
                Month = d.Month,
                StatusCode = statusCode,
                Count = orderCountRows
                    .Where(r => r.Year == d.Year && r.Month == d.Month && r.StatusCode == statusCode)
                    .Sum(r => r.OrderCount)
            }))
            .ToList();

        return new DashboardSummaryDto
        {
            PendingApprovalsCount = pendingApprovalsCount,
            BelowParCount = belowParCount,
            SpendThisMonth = monthlySpend[^1].Total,
            SpendLastMonth = monthlySpend[^2].Total,
            SpendCurrencyCode = currencyCode,
            MonthlySpend = monthlySpend,
            OrderCountsByMonth = orderCountsByMonth,
            ActiveUserCount = activeUserSummary?.ActiveUserCount ?? 0,
            ActiveOrganizationCount = activeUserSummary?.ActiveOrganizationCount ?? 0,
            RecentActivity = recentActivity
        };
    }
}
