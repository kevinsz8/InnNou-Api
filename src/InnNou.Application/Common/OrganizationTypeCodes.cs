namespace InnNou.Application.Common
{
    public static class OrganizationTypeCodes
    {
        public const string SuperAssociate = "SUPER_ASSOCIATE";
        public const string Associate = "ASSOCIATE";

        public static readonly IReadOnlySet<string> All = new HashSet<string> { SuperAssociate, Associate };

        public static bool IsValid(string? code) => code is not null && All.Contains(code.Trim().ToUpperInvariant());
    }
}
