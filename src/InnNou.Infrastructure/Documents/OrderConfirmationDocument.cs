using InnNou.Infrastructure.Repositories.DbEntities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InnNou.Infrastructure.Documents
{
    // Renders the order-confirmation PDF via QuestPDF (no interface wrapper — a pure rendering
    // concern, same "used directly, not behind an abstraction" convention as ClosedXML elsewhere
    // in this codebase). Two entry points share one layout: the full order (every supplier) for
    // the buyer, and a single-supplier slice for that supplier's own email.
    public static class OrderConfirmationDocument
    {
        public static byte[] BuildFullOrderPdf(Order order, string organizationName, List<OrderLine> lines)
            => Render(order, organizationName, "Order Confirmation", OrderConfirmationData.GroupBySupplier(lines), OrderConfirmationData.TotalsByCurrency(lines));

        public static byte[] BuildSupplierPdf(Order order, string organizationName, string supplierName, List<OrderLine> supplierLines)
        {
            var totals = OrderConfirmationData.TotalsByCurrency(supplierLines);
            var group = new OrderConfirmationData.SupplierGroup { SupplierName = supplierName, Lines = supplierLines, SubtotalsByCurrency = totals };
            return Render(order, organizationName, "Purchase Order", [group], totals);
        }

        private static byte[] Render(Order order, string organizationName, string title, List<OrderConfirmationData.SupplierGroup> groups, Dictionary<string, decimal> grandTotals)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("InnNou").FontSize(20).Bold().FontColor(Colors.Teal.Darken2);
                        col.Item().Text(title).FontSize(14).SemiBold();
                        col.Item().PaddingTop(5).Text($"{organizationName} — {order.WarehouseName}").FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"Order #{order.OrderToken.ToString()[..8].ToUpperInvariant()} · {(order.SubmittedUtc ?? DateTime.UtcNow):yyyy-MM-dd HH:mm} UTC")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        foreach (var group in groups)
                        {
                            col.Item().PaddingTop(16).PaddingBottom(8).Text(group.SupplierName)
                                .FontSize(12).Bold().FontColor(Colors.Teal.Darken2);

                            col.Item().PaddingBottom(6).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1.3f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("Article").Bold();
                                    header.Cell().Element(HeaderCell).Text("Qty").Bold();
                                    header.Cell().Element(HeaderCell).Text("Unit").Bold();
                                    header.Cell().Element(HeaderCell).Text("Unit Price").Bold();
                                    header.Cell().Element(HeaderCell).AlignRight().Text("Line Total").Bold();
                                });

                                foreach (var line in group.Lines)
                                {
                                    var lineTotal = line.Quantity * line.UnitPrice;
                                    table.Cell().Element(BodyCell).Text(line.ArticleName ?? "—");
                                    table.Cell().Element(BodyCell).Text(line.Quantity.ToString("0.####"));
                                    table.Cell().Element(BodyCell).Text(line.PurchaseUnitCode ?? "—");
                                    table.Cell().Element(BodyCell).Text($"{line.UnitPrice:0.00} {line.CurrencyCode}");
                                    table.Cell().Element(BodyCell).AlignRight().Text($"{lineTotal:0.00} {line.CurrencyCode}");
                                }
                            });

                            foreach (var (currency, subtotal) in group.SubtotalsByCurrency)
                                col.Item().AlignRight().Text($"Subtotal: {subtotal:0.00} {currency}").SemiBold();
                        }

                        col.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        foreach (var (currency, total) in grandTotals)
                            col.Item().PaddingTop(5).AlignRight().Text($"Grand Total: {total:0.00} {currency}").FontSize(13).Bold();
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated by InnNou — ").FontSize(8).FontColor(Colors.Grey.Darken1);
                        text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            return document.GeneratePdf();
        }

        // Table cells have zero padding/border by default in QuestPDF — without these, the
        // header row visually blends into both the supplier name above it and the data rows
        // below it. A shaded, bottom-bordered header plus consistent cell padding is what makes
        // "supplier name / column headers / rows" read as three distinct visual bands.
        private static IContainer HeaderCell(IContainer container)
            => container
                .Background(Colors.Grey.Lighten3)
                .BorderBottom(1).BorderColor(Colors.Grey.Medium)
                .PaddingVertical(6).PaddingHorizontal(4);

        private static IContainer BodyCell(IContainer container)
            => container
                .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(5).PaddingHorizontal(4);
    }
}
