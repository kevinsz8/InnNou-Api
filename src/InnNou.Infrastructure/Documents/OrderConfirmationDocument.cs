using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InnNou.Infrastructure.Documents
{
    // Renders the order-confirmation PDF via QuestPDF (no interface wrapper — a pure rendering
    // concern, same "used directly, not behind an abstraction" convention as ClosedXML elsewhere
    // in this codebase). Two entry points share one layout: the full order (every supplier) for
    // the buyer, and a single-supplier slice for that supplier's own email.
    //
    // `languageCode` drives every label/heading via OrderConfirmationLocalization. BuildFullOrderPdf's
    // caller passes the buying Organization's own LanguageCode; BuildSupplierPdf's caller passes the
    // Supplier's own LanguageCode. The "en" default only matters when a caller has no code on file.
    public static class OrderConfirmationDocument
    {
        public static byte[] BuildFullOrderPdf(Order order, string organizationName, List<OrderLine> lines, OrderConfirmationData.WarehouseHeaderInfo? warehouseInfo, string? languageCode = "en")
            => Render(order, organizationName, OrderConfirmationLocalization.Label("OrderConfirmationTitle", languageCode), OrderConfirmationData.GroupBySupplier(lines), OrderConfirmationData.TotalsByCurrency(lines), warehouseInfo, languageCode);

        // Shows the real PurchaseOrderNumber (PO-2026-00042) instead of the Order's own token
        // slice — this is what the supplier actually receives, and what they should reference
        // back in a call/email about it, not an internal cart-level identifier they never see
        // anywhere else.
        public static byte[] BuildSupplierPdf(Order order, string organizationName, string supplierName, string purchaseOrderNumber, List<OrderLine> supplierLines, OrderConfirmationData.WarehouseHeaderInfo? warehouseInfo, string? languageCode = "en")
        {
            var totals = OrderConfirmationData.TotalsByCurrency(supplierLines);
            var group = new OrderConfirmationData.SupplierGroup { SupplierName = supplierName, Lines = supplierLines, SubtotalsByCurrency = totals };
            return Render(order, organizationName, OrderConfirmationLocalization.Label("PurchaseOrderTitle", languageCode), [group], totals, warehouseInfo, languageCode, purchaseOrderNumber);
        }

        private static byte[] Render(Order order, string organizationName, string title, List<OrderConfirmationData.SupplierGroup> groups, Dictionary<string, decimal> grandTotals, OrderConfirmationData.WarehouseHeaderInfo? warehouseInfo, string? languageCode, string? referenceOverride = null)
        {
            var orderReference = referenceOverride ?? order.OrderToken.ToString()[..8].ToUpperInvariant();
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
                        col.Item().PaddingBottom(4).Text(title).FontSize(14).SemiBold();

                        // Every header fact is a labeled "Label: value" row — with only Order #/
                        // Warehouse/Organization this read fine unlabeled, but now that Address/
                        // Contact/Phone/Email join the header, each value needs its own label to
                        // stay unambiguous. Rows for data the warehouse doesn't have on file are
                        // skipped entirely (LabeledLine no-ops on a blank value).
                        LabeledLine(col, OrderConfirmationLocalization.Label("OrderNumberLabel", languageCode), orderReference);
                        LabeledLine(col, OrderConfirmationLocalization.Label("DateLabel", languageCode), $"{(order.SubmittedUtc ?? DateTime.UtcNow):yyyy-MM-dd HH:mm} UTC");
                        LabeledLine(col, OrderConfirmationLocalization.Label("OrganizationLabel", languageCode), organizationName);
                        LabeledLine(col, OrderConfirmationLocalization.Label("WarehouseLabel", languageCode), order.WarehouseName);
                        LabeledLine(col, OrderConfirmationLocalization.Label("AddressLabel", languageCode), OrderConfirmationData.FormatAddress(warehouseInfo));
                        LabeledLine(col, OrderConfirmationLocalization.Label("ContactLabel", languageCode), warehouseInfo?.ContactName);
                        LabeledLine(col, OrderConfirmationLocalization.Label("PhoneLabel", languageCode), warehouseInfo?.ContactPhone);
                        LabeledLine(col, OrderConfirmationLocalization.Label("EmailLabel", languageCode), warehouseInfo?.ContactEmail);
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
                                    header.Cell().Element(HeaderCell).Text(OrderConfirmationLocalization.Label("ArticleColumn", languageCode)).Bold();
                                    header.Cell().Element(HeaderCell).Text(OrderConfirmationLocalization.Label("QtyColumn", languageCode)).Bold();
                                    header.Cell().Element(HeaderCell).Text(OrderConfirmationLocalization.Label("UnitColumn", languageCode)).Bold();
                                    header.Cell().Element(HeaderCell).Text(OrderConfirmationLocalization.Label("UnitPriceColumn", languageCode)).Bold();
                                    header.Cell().Element(HeaderCell).AlignRight().Text(OrderConfirmationLocalization.Label("LineTotalColumn", languageCode)).Bold();
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
                                col.Item().AlignRight().Text($"{OrderConfirmationLocalization.Label("SubtotalLabel", languageCode)}: {subtotal:0.00} {currency}").SemiBold();
                        }

                        col.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        foreach (var (currency, total) in grandTotals)
                            col.Item().PaddingTop(5).AlignRight().Text($"{OrderConfirmationLocalization.Label("GrandTotalLabel", languageCode)}: {total:0.00} {currency}").FontSize(13).Bold();
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span($"{OrderConfirmationLocalization.Label("GeneratedByLabel", languageCode)} ").FontSize(8).FontColor(Colors.Grey.Darken1);
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
