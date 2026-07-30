namespace InnNou.Application.Common
{
    // Underlying int values must match SupplierReturnResolutionTypes seed rows exactly
    // (see database/migrations/20260730_SupplierReturns_Create.sql).
    //
    // Written_Off (not WrittenOff) is deliberate — same Dapper enum-binding gotcha documented
    // on OrderStatus.Pending_Approval: Dapper's built-in enum-column binding does not ignore
    // underscores, so the member name must match the Code string's spelling exactly.
    public enum SupplierReturnResolutionType
    {
        Credited = 1,
        Replaced = 2,
        Written_Off = 3
    }

    public static class SupplierReturnResolutionTypeCodes
    {
        public const string Credited = "CREDITED";
        public const string Replaced = "REPLACED";
        public const string WrittenOff = "WRITTEN_OFF";

        public static readonly string[] All = [Credited, Replaced, WrittenOff];

        public static string ToCode(SupplierReturnResolutionType type) => type switch
        {
            SupplierReturnResolutionType.Credited => Credited,
            SupplierReturnResolutionType.Replaced => Replaced,
            SupplierReturnResolutionType.Written_Off => WrittenOff,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public static bool TryFromCode(string? code, out SupplierReturnResolutionType type)
        {
            switch (code)
            {
                case Credited: type = SupplierReturnResolutionType.Credited; return true;
                case Replaced: type = SupplierReturnResolutionType.Replaced; return true;
                case WrittenOff: type = SupplierReturnResolutionType.Written_Off; return true;
                default: type = default; return false;
            }
        }
    }
}
