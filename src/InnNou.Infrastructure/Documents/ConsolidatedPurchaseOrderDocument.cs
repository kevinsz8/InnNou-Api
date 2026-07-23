using InnNou.Infrastructure.Repositories.DbEntities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InnNou.Infrastructure.Documents
{
    // Renders the multi-property consolidation PDF via QuestPDF — an aggregate-by-article view
    // (for negotiating with the supplier) plus a per-property breakdown (so nothing loses
    // traceability back to which property ordered what). Generated on demand from live data,
    // never persisted — unlike OrderConfirmationDocument there's no email-attachment use case
    // here. English-only for V1 — this is an internal group-management artifact, not sent
    // automatically to an external party the way the supplier confirmation email/PDF is.
    public static class ConsolidatedPurchaseOrderDocument
    {
        private sealed class AggregateLine
        {
            public string ArticleName { get; set; } = default!;
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public string CurrencyCode { get; set; } = default!;
            public decimal Total => Quantity * UnitPrice;
        }

        public static byte[] Build(ConsolidatedPurchaseOrder header, List<(ConsolidatedPurchaseOrderMember Member, List<PurchaseOrderLine> Lines)> memberLines)
        {
            // Grouped by (Article, UnitPrice, Currency) — different properties may have
            // negotiated different prices for the same article, so a single blended average
            // would misrepresent what's actually being paid; showing each price point
            // separately is the honest representation.
            var aggregate = memberLines
                .SelectMany(m => m.Lines.Where(l => !l.IsCancelled))
                .GroupBy(l => new { Article = l.ArticleName ?? "—", l.UnitPrice, l.CurrencyCode })
                .Select(g => new AggregateLine
                {
                    ArticleName = g.Key.Article,
                    Quantity = g.Sum(l => l.Quantity),
                    UnitPrice = g.Key.UnitPrice,
                    CurrencyCode = g.Key.CurrencyCode
                })
                .OrderBy(a => a.ArticleName)
                .ToList();

            var grandTotalsByCurrency = memberLines
                .SelectMany(m => m.Lines.Where(l => !l.IsCancelled))
                .GroupBy(l => l.CurrencyCode)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity * l.UnitPrice));

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
                        col.Item().PaddingBottom(4).Text("Consolidated Purchase Order").FontSize(14).SemiBold();
                        if (!string.IsNullOrWhiteSpace(header.Title))
                            col.Item().PaddingBottom(2).Text(header.Title!).FontSize(11).SemiBold();

                        LabeledLine(col, "Supplier", header.SupplierName);
                        LabeledLine(col, "Group", header.SuperAssociateOrganizationName);
                        LabeledLine(col, "Period", $"{header.DateRangeFrom:yyyy-MM-dd} to {header.DateRangeTo:yyyy-MM-dd}");
                        LabeledLine(col, "Properties", memberLines.Count.ToString());
                        if (!string.IsNullOrWhiteSpace(header.Notes))
                            LabeledLine(col, "Notes", header.Notes);
                    });

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().PaddingBottom(8).Text("Aggregate by article").FontSize(12).Bold().FontColor(Colors.Teal.Darken2);
                        col.Item().PaddingBottom(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1.3f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(headerRow =>
                            {
                                headerRow.Cell().Element(HeaderCell).Text("Article").Bold();
                                headerRow.Cell().Element(HeaderCell).Text("Total Qty").Bold();
                                headerRow.Cell().Element(HeaderCell).Text("Unit Price").Bold();
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("Total").Bold();
                            });

                            foreach (var line in aggregate)
                            {
                                table.Cell().Element(BodyCell).Text(line.ArticleName);
                                table.Cell().Element(BodyCell).Text(line.Quantity.ToString("0.####"));
                                table.Cell().Element(BodyCell).Text($"{line.UnitPrice:0.00} {line.CurrencyCode}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{line.Total:0.00} {line.CurrencyCode}");
                            }
                        });

                        col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        foreach (var (currency, total) in grandTotalsByCurrency)
                            col.Item().PaddingTop(5).AlignRight().Text($"Grand total: {total:0.00} {currency}").FontSize(13).Bold();

                        col.Item().PaddingTop(20).PaddingBottom(8).Text("Breakdown by property").FontSize(12).Bold().FontColor(Colors.Teal.Darken2);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.8f);
                            });

                            table.Header(headerRow =>
                            {
                                headerRow.Cell().Element(HeaderCell).Text("Property").Bold();
                                headerRow.Cell().Element(HeaderCell).Text("PO #").Bold();
                                headerRow.Cell().Element(HeaderCell).Text("Lines").Bold();
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("Subtotal").Bold();
                            });

                            foreach (var (member, lines) in memberLines)
                            {
                                var activeLines = lines.Where(l => !l.IsCancelled).ToList();
                                var subtotals = activeLines
                                    .GroupBy(l => l.CurrencyCode)
                                    .Select(g => $"{g.Sum(l => l.Quantity * l.UnitPrice):0.00} {g.Key}");

                                table.Cell().Element(BodyCell).Text(member.OrganizationName ?? "—");
                                table.Cell().Element(BodyCell).Text(member.PurchaseOrderNumber);
                                table.Cell().Element(BodyCell).Text(activeLines.Count.ToString());
                                table.Cell().Element(BodyCell).AlignRight().Text(string.Join(", ", subtotals));
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated ").FontSize(8).FontColor(Colors.Grey.Darken1);
                        text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static void LabeledLine(ColumnDescriptor col, string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            col.Item().Text(t =>
            {
                t.Span($"{label}: ").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken2);
                t.Span(value).FontSize(9).FontColor(Colors.Grey.Darken1);
            });
        }

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
