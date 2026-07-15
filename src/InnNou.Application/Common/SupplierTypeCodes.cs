namespace InnNou.Application.Common
{
    // Informational-only classification of what a Supplier provides. Does not drive any
    // business logic yet — Article's fixed-unit purchase/content structure still assumes a
    // product regardless of the owning supplier's type. Modeling variable-priced services
    // properly (e.g. a contractor billing per job rather than per catalog unit) is a
    // separate, not-yet-designed follow-up.
    public static class SupplierTypeCodes
    {
        public const string Product = "PRODUCT";
        public const string Service = "SERVICE";
        public const string Mixed = "MIXED";

        public static readonly IReadOnlySet<string> All = new HashSet<string> { Product, Service, Mixed };

        public static bool IsValid(string? code) => code is not null && All.Contains(code.Trim().ToUpperInvariant());
    }
}
