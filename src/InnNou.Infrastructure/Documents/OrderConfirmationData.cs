using InnNou.Infrastructure.Repositories.DbEntities;

namespace InnNou.Infrastructure.Documents
{
    // Shared grouping/totals logic for OrderConfirmationDocument (PDF) and
    // OrderConfirmationEmailContent (HTML) — kept in one place so the two never drift apart.
    // Totals are computed per currency, never summed across currencies: in practice an Order's
    // lines all resolve to the same currency (the org's own resolved currency, or a manually
    // supplied one — see OrderService.AddLineAsync), but nothing here may assume that silently.
    public static class OrderConfirmationData
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

        // The delivery warehouse's address + primary contact — surfaced in the PDF/email header
        // so a recipient can tell at a glance where/who the order is for, alongside Order #/
        // Organization/Warehouse. All fields optional: a warehouse with no address/contact on
        // file simply omits that labeled row rather than printing an empty one.
        public sealed class WarehouseHeaderInfo
        {
            public string? AddressLine1 { get; init; }
            public string? AddressLine2 { get; init; }
            public string? City { get; init; }
            public string? State { get; init; }
            public string? PostalCode { get; init; }
            public string? Country { get; init; }
            public string? ContactName { get; init; }
            public string? ContactPhone { get; init; }
            public string? ContactEmail { get; init; }
        }

        public static string? FormatAddress(WarehouseHeaderInfo? info)
        {
            if (info is null) return null;

            var parts = new[] { info.AddressLine1, info.AddressLine2, info.City, info.State, info.PostalCode, info.Country }
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var joined = string.Join(", ", parts);
            return string.IsNullOrWhiteSpace(joined) ? null : joined;
        }
    }
}
