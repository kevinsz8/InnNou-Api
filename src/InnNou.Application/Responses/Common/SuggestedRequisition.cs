namespace InnNou.Application.Responses.Common
{
    public class SuggestedRequisition
    {
        public Guid DepartmentParLevelToken { get; set; }
        public Guid DepartmentToken { get; set; }
        public string? DepartmentName { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public decimal MinimumQuantity { get; set; }
        public decimal SuggestedQuantity { get; set; }
        public decimal AvgDailyConsumption { get; set; }
        public DateTime LastIssuedUtc { get; set; }
        public int DaysSinceLastIssued { get; set; }
        public decimal ExpectedCycleDays { get; set; }
    }
}
