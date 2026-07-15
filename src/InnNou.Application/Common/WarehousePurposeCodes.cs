namespace InnNou.Application.Common
{
    // Informational-only classification for UI filtering/reporting — application code must
    // never branch business behavior on this value, only on Warehouse's capability flags.
    public static class WarehousePurposeCodes
    {
        public const string General = "GENERAL";
        public const string Storage = "STORAGE";
        public const string Kitchen = "KITCHEN";
        public const string Restaurant = "RESTAURANT";
        public const string Bar = "BAR";
        public const string Housekeeping = "HOUSEKEEPING";
        public const string Maintenance = "MAINTENANCE";
        public const string Office = "OFFICE";
        public const string Production = "PRODUCTION";
        public const string Transit = "TRANSIT";
        public const string Waste = "WASTE";
        public const string Virtual = "VIRTUAL";
        public const string Other = "OTHER";

        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            General, Storage, Kitchen, Restaurant, Bar, Housekeeping,
            Maintenance, Office, Production, Transit, Waste, Virtual, Other
        };

        public static bool IsValid(string? code) => code is not null && All.Contains(code.Trim().ToUpperInvariant());
    }
}
