namespace InnNou.Application.Common
{
    // Underlying int values must match InventoryPeriodStatuses.InventoryPeriodStatusId seed rows
    // exactly (see database/migrations/20260729_InventoryPeriods_Create.sql).
    //
    // In_Progress/Pre_Closed keep the underscore (not PascalCase "InProgress"/"PreClosed")
    // because Dapper's default string-to-enum column mapping matches the enum MEMBER NAME
    // against the SQL row's string value case-insensitively but does NOT strip underscores —
    // same real bug hit and fixed for OrderStatus.Pending_Approval / PurchaseOrderStatus's
    // Partially_Received. Renaming these to PascalCase would silently break deserializing any
    // row with Status = 'IN_PROGRESS'/'PRE_CLOSED'.
    public enum InventoryPeriodStatus
    {
        Open = 1,
        In_Progress = 2,
        Pre_Closed = 3,
        Closed = 4
    }

    public static class InventoryPeriodStatusCodes
    {
        public const string Open = "OPEN";
        public const string InProgress = "IN_PROGRESS";
        public const string PreClosed = "PRE_CLOSED";
        public const string Closed = "CLOSED";

        public static string ToCode(InventoryPeriodStatus status) => status switch
        {
            InventoryPeriodStatus.Open => Open,
            InventoryPeriodStatus.In_Progress => InProgress,
            InventoryPeriodStatus.Pre_Closed => PreClosed,
            InventoryPeriodStatus.Closed => Closed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        public static InventoryPeriodStatus FromCode(string code) => code.Trim().ToUpperInvariant() switch
        {
            Open => InventoryPeriodStatus.Open,
            InProgress => InventoryPeriodStatus.In_Progress,
            PreClosed => InventoryPeriodStatus.Pre_Closed,
            Closed => InventoryPeriodStatus.Closed,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };

        // Non-throwing variant for caller-supplied filter values (e.g. GetPaged's optional
        // status filter) — an unrecognized code should just match nothing, not 500.
        public static bool TryFromCode(string? code, out InventoryPeriodStatus status)
        {
            switch (code?.Trim().ToUpperInvariant())
            {
                case Open: status = InventoryPeriodStatus.Open; return true;
                case InProgress: status = InventoryPeriodStatus.In_Progress; return true;
                case PreClosed: status = InventoryPeriodStatus.Pre_Closed; return true;
                case Closed: status = InventoryPeriodStatus.Closed; return true;
                default: status = default; return false;
            }
        }
    }
}
