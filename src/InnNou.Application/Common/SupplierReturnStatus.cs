namespace InnNou.Application.Common
{
    // Underlying int values must match SupplierReturnStatuses seed rows exactly
    // (see database/migrations/20260730_SupplierReturns_Create.sql).
    public enum SupplierReturnStatus
    {
        Pending = 1,
        Closed = 2
    }

    public static class SupplierReturnStatusCodes
    {
        public const string Pending = "PENDING";
        public const string Closed = "CLOSED";

        public static string ToCode(SupplierReturnStatus status) => status switch
        {
            SupplierReturnStatus.Pending => Pending,
            SupplierReturnStatus.Closed => Closed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        public static bool TryFromCode(string? code, out SupplierReturnStatus status)
        {
            switch (code)
            {
                case Pending: status = SupplierReturnStatus.Pending; return true;
                case Closed: status = SupplierReturnStatus.Closed; return true;
                default: status = default; return false;
            }
        }
    }
}
