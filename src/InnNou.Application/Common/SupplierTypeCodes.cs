namespace InnNou.Application.Common
{
    // Classification of what a Supplier provides. Drives OrderService.AddLineAsync's
    // manual-price fallback (SERVICE/MIXED may supply a caller-priced line when no
    // catalog ArticlePrice resolves; PRODUCT always hard-fails instead).
    //
    // Underlying int values must match SupplierTypes.SupplierTypeId seed rows exactly
    // (see database/migrations/20260722_SupplierTypes_ConvertToId.sql).
    public enum SupplierType
    {
        Product = 1,
        Service = 2,
        Mixed = 3
    }

    public static class SupplierTypeCodes
    {
        public const string Product = "PRODUCT";
        public const string Service = "SERVICE";
        public const string Mixed = "MIXED";

        public static readonly IReadOnlySet<string> All = new HashSet<string> { Product, Service, Mixed };

        public static bool IsValid(string? code) => code is not null && All.Contains(code.Trim().ToUpperInvariant());

        public static string ToCode(SupplierType type) => type switch
        {
            SupplierType.Product => Product,
            SupplierType.Service => Service,
            SupplierType.Mixed => Mixed,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public static SupplierType FromCode(string code) => code.Trim().ToUpperInvariant() switch
        {
            Product => SupplierType.Product,
            Service => SupplierType.Service,
            Mixed => SupplierType.Mixed,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };
    }
}
