using InnNou.Infrastructure.Repositories.DbEntities;

namespace InnNou.Infrastructure.Documents
{
    // Shared grouping/totals logic for OrderConfirmationDocument (PDF) and
    // OrderConfirmationEmailContent (HTML) — kept in one place so the two never drift apart.
    // Totals are computed per currency, never summed across currencies: in practice an Order's
    // lines all resolve to the same currency (the org's own resolved currency, or a manually
    // supplied one — see OrderService.AddLineAsync), but nothing here may assume that silently.
    internal static class OrderConfirmationData
    {
        public sealed class SupplierGroup
        {
            public required string SupplierName { get; init; }
            public required List<OrderLine> Lines { get; init; }
            public required Dictionary<string, decimal> SubtotalsByCurrency { get; init; }
        }

        public static List<SupplierGroup> GroupBySupplier(List<OrderLine> lines)
        {
            return lines
                .GroupBy(l => new { l.SupplierId, l.SupplierName })
                .Select(g => new SupplierGroup
                {
                    SupplierName = g.Key.SupplierName ?? "—",
                    Lines = g.ToList(),
                    SubtotalsByCurrency = TotalsByCurrency(g.ToList())
                })
                .ToList();
        }

        public static Dictionary<string, decimal> TotalsByCurrency(List<OrderLine> lines)
        {
            return lines
                .GroupBy(l => l.CurrencyCode)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity * l.UnitPrice));
        }
    }
}
