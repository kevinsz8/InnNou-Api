namespace InnNou.Application.Common
{
    // Underlying int values must match SupplierInvoiceStatuses seed rows exactly
    // (see database/migrations/20260730_SupplierInvoices_Create.sql).
    public enum SupplierInvoiceStatus
    {
        Matched = 1,
        Discrepancy = 2
    }

    public static class SupplierInvoiceStatusCodes
    {
        public const string Matched = "MATCHED";
        public const string Discrepancy = "DISCREPANCY";

        public static readonly string[] All = [Matched, Discrepancy];

        public static string ToCode(SupplierInvoiceStatus status) => status switch
        {
            SupplierInvoiceStatus.Matched => Matched,
            SupplierInvoiceStatus.Discrepancy => Discrepancy,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        public static bool TryFromCode(string? code, out SupplierInvoiceStatus status)
        {
            switch (code)
            {
                case Matched: status = SupplierInvoiceStatus.Matched; return true;
                case Discrepancy: status = SupplierInvoiceStatus.Discrepancy; return true;
                default: status = default; return false;
            }
        }
    }
}
