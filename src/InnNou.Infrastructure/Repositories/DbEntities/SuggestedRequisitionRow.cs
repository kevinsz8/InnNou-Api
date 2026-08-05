namespace InnNou.Infrastructure.Repositories.DbEntities
{
    // Result shape of sp_DepartmentParLevel_GetSuggested — deliberately not a DepartmentParLevel
    // subclass (unlike the usual XPageRow : X pattern), since the consumption-derived columns
    // (AvgDailyConsumption/DaysSinceLastIssued/ExpectedCycleDays) have no equivalent on the base
    // configuration row at all.
    public class SuggestedRequisitionRow
    {
        public int DepartmentParLevelId { get; set; }
        public Guid DepartmentParLevelToken { get; set; }
        public int DepartmentId { get; set; }
        public Guid DepartmentToken { get; set; }
        public string? DepartmentName { get; set; }
        public int OrganizationId { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? PurchaseUnitCode { get; set; }

        public decimal MinimumQuantity { get; set; }
        public decimal SuggestedQuantity { get; set; }

        public decimal TotalConsumed90d { get; set; }
        public decimal AvgDailyConsumption { get; set; }
        public DateTime LastIssuedUtc { get; set; }
        public int DaysSinceLastIssued { get; set; }
        public decimal ExpectedCycleDays { get; set; }

        public int TotalCount { get; set; }
    }
}
