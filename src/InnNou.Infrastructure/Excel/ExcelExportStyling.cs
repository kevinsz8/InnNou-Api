using ClosedXML.Excel;

namespace InnNou.Infrastructure.Excel
{
    // Shared "InnNou look" for every bulk-export/import-template worksheet, so every generated
    // document in the app reads as one family — colors mirror the table-header treatment already
    // established in Documents/OrderConfirmationDocument.cs (Colors.Grey.Lighten3 fill / Grey.Medium
    // border in QuestPDF terms). Kept as static styling calls with no interface, same "used directly"
    // convention as ClosedXML itself elsewhere in this codebase.
    public static class ExcelExportStyling
    {
        private static readonly XLColor HeaderFill = XLColor.FromHtml("#EEEEEE");
        private static readonly XLColor HeaderBorder = XLColor.FromHtml("#9E9E9E");
        private static readonly XLColor GridBorder = XLColor.FromHtml("#E0E0E0");
        private static readonly XLColor ActiveFill = XLColor.FromHtml("#C8E6C9");
        private static readonly XLColor ActiveFont = XLColor.FromHtml("#2E7D32");
        private static readonly XLColor InactiveFill = XLColor.FromHtml("#EEEEEE");
        private static readonly XLColor InactiveFont = XLColor.FromHtml("#616161");
        private const double MaxColumnWidth = 42;

        // Call once per worksheet, right before saving, after every header/data cell has been
        // written. Styles the header row (bold, brand-grey fill, bottom border, frozen so it stays
        // visible while scrolling), draws a light grid over the data range, wires an autofilter
        // across the whole table, and caps AdjustToContents so one long free-text value
        // (Description, Notes) can't blow a column out to an unusable width. lastDataRow is the
        // header row itself (no data yet) for an empty result set.
        public static void FinalizeWorksheet(IXLWorksheet worksheet, int columnCount, int lastDataRow, int headerRow = 1)
        {
            var headerRange = worksheet.Range(headerRow, 1, headerRow, columnCount);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = HeaderFill;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            headerRange.Style.Border.BottomBorderColor = HeaderBorder;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.SheetView.FreezeRows(headerRow);

            if (lastDataRow > headerRow)
            {
                var dataRange = worksheet.Range(headerRow + 1, 1, lastDataRow, columnCount);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.OutsideBorderColor = GridBorder;
                dataRange.Style.Border.InsideBorderColor = GridBorder;
            }

            worksheet.Range(headerRow, 1, lastDataRow, columnCount).SetAutoFilter();

            worksheet.Columns(1, columnCount).AdjustToContents();
            foreach (var column in worksheet.Columns(1, columnCount))
            {
                if (column.Width > MaxColumnWidth)
                    column.Width = MaxColumnWidth;
            }
        }

        // Status column color-coding — Active/Inactive reads at a glance without reading the text on
        // a long list. Text value itself is left untouched (still the untranslated "Active"/
        // "Inactive" — see CLAUDE.md's bulk-import localization scope note: headers only, never data
        // values), this only adds a fill/font color on top of it.
        public static void StyleStatusCell(IXLCell cell, bool isActive)
        {
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = isActive ? ActiveFill : InactiveFill;
            cell.Style.Font.FontColor = isActive ? ActiveFont : InactiveFont;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        public static void ApplyMoneyFormat(IXLCell cell) => cell.Style.NumberFormat.Format = "#,##0.00";

        public static void ApplyQuantityFormat(IXLCell cell) => cell.Style.NumberFormat.Format = "#,##0.####";

        public static void ApplyDateFormat(IXLCell cell) => cell.Style.NumberFormat.Format = "yyyy-mm-dd";

        public static void ApplyDateTimeFormat(IXLCell cell) => cell.Style.NumberFormat.Format = "yyyy-mm-dd hh:mm";
    }
}
