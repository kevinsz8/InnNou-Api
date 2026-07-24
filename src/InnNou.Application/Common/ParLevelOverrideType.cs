namespace InnNou.Application.Common
{
    // Underlying int values must match ParLevelOverrideTypes.ParLevelOverrideTypeId seed rows
    // exactly (see database/migrations/20260728_ParLevels_Create.sql).
    public enum ParLevelOverrideType
    {
        Seasonal = 1,
        Event = 2
    }

    public static class ParLevelOverrideTypeCodes
    {
        public const string Seasonal = "SEASONAL";
        public const string Event = "EVENT";

        public static string ToCode(ParLevelOverrideType type) => type switch
        {
            ParLevelOverrideType.Seasonal => Seasonal,
            ParLevelOverrideType.Event => Event,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public static ParLevelOverrideType FromCode(string code) => code.Trim().ToUpperInvariant() switch
        {
            Seasonal => ParLevelOverrideType.Seasonal,
            Event => ParLevelOverrideType.Event,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };
    }
}
