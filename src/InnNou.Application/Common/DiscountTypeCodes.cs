namespace InnNou.Application.Common
{
    // Underlying int values must match DiscountTypes.DiscountTypeId seed rows exactly
    // (see database/migrations/20260807_ArticleDiscounts_Create.sql).
    public enum DiscountType
    {
        Percentage = 1,
        FixedAmount = 2
    }

    public static class DiscountTypeCodes
    {
        public const string Percentage = "PERCENTAGE";
        public const string FixedAmount = "FIXED_AMOUNT";

        public static readonly IReadOnlySet<string> All = new HashSet<string> { Percentage, FixedAmount };

        public static bool IsValid(string? code) => code is not null && All.Contains(code.Trim().ToUpperInvariant());

        public static string ToCode(DiscountType type) => type switch
        {
            DiscountType.Percentage => Percentage,
            DiscountType.FixedAmount => FixedAmount,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public static DiscountType FromCode(string code) => code.Trim().ToUpperInvariant() switch
        {
            Percentage => DiscountType.Percentage,
            FixedAmount => DiscountType.FixedAmount,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };
    }
}
