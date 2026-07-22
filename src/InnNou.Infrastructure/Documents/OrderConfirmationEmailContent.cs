using InnNou.Infrastructure.Repositories.DbEntities;
using System.Text;

namespace InnNou.Infrastructure.Documents
{
    // Builds the HTML email bodies for order confirmation — reuses OrderConfirmationData's
    // grouping/totals so the email and the attached PDF never disagree on numbers.
    public static class OrderConfirmationEmailContent
    {
        public static string BuildBuyerEmailHtml(Order order, string organizationName, List<OrderLine> lines)
            => Render(order, organizationName, "Order confirmed", OrderConfirmationData.GroupBySupplier(lines), OrderConfirmationData.TotalsByCurrency(lines));

        public static string BuildSupplierEmailHtml(Order order, string organizationName, string supplierName, List<OrderLine> supplierLines)
        {
            var totals = OrderConfirmationData.TotalsByCurrency(supplierLines);
            var group = new OrderConfirmationData.SupplierGroup { SupplierName = supplierName, Lines = supplierLines, SubtotalsByCurrency = totals };
            return Render(order, organizationName, "New purchase order", [group], totals);
        }

        private static string Render(Order order, string organizationName, string heading, List<OrderConfirmationData.SupplierGroup> groups, Dictionary<string, decimal> grandTotals)
        {
            var orderReference = order.OrderToken.ToString()[..8].ToUpperInvariant();
            var submittedUtc = order.SubmittedUtc ?? DateTime.UtcNow;

            var sb = new StringBuilder();
            sb.Append($$"""
                <div style="font-family:Segoe UI,Arial,sans-serif;max-width:640px;margin:0 auto;color:#1f2937;">
                  <div style="background:#0C8470;padding:20px 24px;border-radius:8px 8px 0 0;">
                    <div style="color:#ffffff;font-size:20px;font-weight:600;">InnNou</div>
                    <div style="color:#d1fae5;font-size:13px;">{{heading}}</div>
                  </div>
                  <div style="border:1px solid #e5e7eb;border-top:none;padding:20px 24px;border-radius:0 0 8px 8px;">
                    <p style="font-size:13px;color:#374151;margin:0 0 4px;"><strong>{{organizationName}}</strong> — {{order.WarehouseName}}</p>
                    <p style="font-size:12px;color:#6b7280;margin:0 0 16px;">Order #{{orderReference}} · {{submittedUtc:yyyy-MM-dd HH:mm}} UTC</p>
                """);

            foreach (var group in groups)
            {
                sb.Append($$"""
                    <h3 style="font-size:14px;margin:16px 0 8px;color:#111827;">{{group.SupplierName}}</h3>
                    <table style="width:100%;border-collapse:collapse;font-size:12.5px;">
                      <thead>
                        <tr style="background:#f3f4f6;text-align:left;">
                          <th style="padding:6px 8px;">Article</th>
                          <th style="padding:6px 8px;">Qty</th>
                          <th style="padding:6px 8px;">Unit price</th>
                          <th style="padding:6px 8px;text-align:right;">Line total</th>
                        </tr>
                      </thead>
                      <tbody>
                    """);

                foreach (var line in group.Lines)
                {
                    var lineTotal = line.Quantity * line.UnitPrice;
                    sb.Append($$"""
                        <tr style="border-bottom:1px solid #f3f4f6;">
                          <td style="padding:6px 8px;">{{line.ArticleName}}</td>
                          <td style="padding:6px 8px;">{{line.Quantity:0.####}} {{line.PurchaseUnitCode}}</td>
                          <td style="padding:6px 8px;">{{line.UnitPrice:0.00}} {{line.CurrencyCode}}</td>
                          <td style="padding:6px 8px;text-align:right;">{{lineTotal:0.00}} {{line.CurrencyCode}}</td>
                        </tr>
                        """);
                }

                sb.Append("</tbody></table>");

                foreach (var (currency, subtotal) in group.SubtotalsByCurrency)
                    sb.Append($"""<p style="text-align:right;font-size:12.5px;font-weight:600;margin:6px 0 0;">Subtotal: {subtotal:0.00} {currency}</p>""");
            }

            sb.Append("""<hr style="border:none;border-top:1px solid #e5e7eb;margin:20px 0 10px;">""");
            foreach (var (currency, total) in grandTotals)
                sb.Append($"""<p style="text-align:right;font-size:15px;font-weight:700;color:#0C8470;margin:4px 0;">Grand total: {total:0.00} {currency}</p>""");

            sb.Append("""
                    <p style="font-size:11px;color:#9ca3af;margin-top:24px;">This is an automated confirmation from InnNou. The full order confirmation is attached as a PDF.</p>
                  </div>
                </div>
                """);

            return sb.ToString();
        }
    }
}
