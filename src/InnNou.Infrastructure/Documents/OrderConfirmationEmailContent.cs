using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Localization;
using System.Text;

namespace InnNou.Infrastructure.Documents
{
    // Builds the HTML email bodies for order confirmation — reuses OrderConfirmationData's
    // grouping/totals so the email and the attached PDF never disagree on numbers.
    //
    // `languageCode` drives every label/heading via OrderConfirmationLocalization. BuildBuyerEmailHtml's
    // caller passes the buying Organization's own LanguageCode; BuildSupplierEmailHtml's caller
    // passes the Supplier's own LanguageCode. The "en" default only matters when a caller has no
    // code on file.
    public static class OrderConfirmationEmailContent
    {
        public static string BuildBuyerEmailHtml(Order order, string organizationName, List<OrderLine> lines, OrderConfirmationData.WarehouseHeaderInfo? warehouseInfo, string? languageCode = "en")
            => Render(order, organizationName, OrderConfirmationLocalization.Label("OrderConfirmedHeading", languageCode), OrderConfirmationData.GroupBySupplier(lines), OrderConfirmationData.TotalsByCurrency(lines), warehouseInfo, languageCode);

        public static string BuildSupplierEmailHtml(Order order, string organizationName, string supplierName, List<OrderLine> supplierLines, OrderConfirmationData.WarehouseHeaderInfo? warehouseInfo, string? languageCode = "en")
        {
            var totals = OrderConfirmationData.TotalsByCurrency(supplierLines);
            var group = new OrderConfirmationData.SupplierGroup { SupplierName = supplierName, Lines = supplierLines, SubtotalsByCurrency = totals };
            return Render(order, organizationName, OrderConfirmationLocalization.Label("NewPurchaseOrderHeading", languageCode), [group], totals, warehouseInfo, languageCode);
        }

        // Every header fact is a labeled row (Order #/Date/Organization/Warehouse/Address/
        // Contact/Phone/Email) — with only Order #/Warehouse/Organization this read fine as plain
        // text, but now that the delivery warehouse's address/contact join the header, each value
        // needs its own label to stay unambiguous. A row for data the warehouse doesn't have on
        // file is skipped entirely.
        private static void AppendHeaderRow(StringBuilder sb, string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            sb.Append($$"""
                <tr>
                  <td style="padding:2px 10px 2px 0;color:#6b7280;white-space:nowrap;vertical-align:top;">{{label}}</td>
                  <td style="padding:2px 0;color:#374151;">{{value}}</td>
                </tr>
                """);
        }

        private static string Render(Order order, string organizationName, string heading, List<OrderConfirmationData.SupplierGroup> groups, Dictionary<string, decimal> grandTotals, OrderConfirmationData.WarehouseHeaderInfo? warehouseInfo, string? languageCode)
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
                    <table style="font-size:12.5px;margin:0 0 16px;border-collapse:collapse;">
                """);

            AppendHeaderRow(sb, OrderConfirmationLocalization.Label("OrderNumberLabel", languageCode), orderReference);
            AppendHeaderRow(sb, OrderConfirmationLocalization.Label("DateLabel", languageCode), $"{submittedUtc:yyyy-MM-dd HH:mm} UTC");
            AppendHeaderRow(sb, OrderConfirmationLocalization.Label("OrganizationLabel", languageCode), organizationName);
            AppendHeaderRow(sb, OrderConfirmationLocalization.Label("WarehouseLabel", languageCode), order.WarehouseName);
            AppendHeaderRow(sb, OrderConfirmationLocalization.Label("AddressLabel", languageCode), OrderConfirmationData.FormatAddress(warehouseInfo));
            AppendHeaderRow(sb, OrderConfirmationLocalization.Label("ContactLabel", languageCode), warehouseInfo?.ContactName);
            AppendHeaderRow(sb, OrderConfirmationLocalization.Label("PhoneLabel", languageCode), warehouseInfo?.ContactPhone);
            AppendHeaderRow(sb, OrderConfirmationLocalization.Label("EmailLabel", languageCode), warehouseInfo?.ContactEmail);

            sb.Append("</table>");

            foreach (var group in groups)
            {
                sb.Append($$"""
                    <h3 style="font-size:14px;margin:16px 0 8px;color:#111827;">{{group.SupplierName}}</h3>
                    <table style="width:100%;border-collapse:collapse;font-size:12.5px;">
                      <thead>
                        <tr style="background:#f3f4f6;text-align:left;">
                          <th style="padding:6px 8px;">{{OrderConfirmationLocalization.Label("ArticleColumn", languageCode)}}</th>
                          <th style="padding:6px 8px;">{{OrderConfirmationLocalization.Label("QtyColumn", languageCode)}}</th>
                          <th style="padding:6px 8px;">{{OrderConfirmationLocalization.Label("UnitPriceColumn", languageCode)}}</th>
                          <th style="padding:6px 8px;text-align:right;">{{OrderConfirmationLocalization.Label("LineTotalColumn", languageCode)}}</th>
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
                    sb.Append($"""<p style="text-align:right;font-size:12.5px;font-weight:600;margin:6px 0 0;">{OrderConfirmationLocalization.Label("SubtotalLabel", languageCode)}: {subtotal:0.00} {currency}</p>""");
            }

            sb.Append("""<hr style="border:none;border-top:1px solid #e5e7eb;margin:20px 0 10px;">""");
            foreach (var (currency, total) in grandTotals)
                sb.Append($"""<p style="text-align:right;font-size:15px;font-weight:700;color:#0C8470;margin:4px 0;">{OrderConfirmationLocalization.Label("GrandTotalLabel", languageCode)}: {total:0.00} {currency}</p>""");

            sb.Append($"""
                    <p style="font-size:11px;color:#9ca3af;margin-top:24px;">{OrderConfirmationLocalization.Label("EmailFooterNote", languageCode)}</p>
                  </div>
                </div>
                """);

            return sb.ToString();
        }
    }
}
