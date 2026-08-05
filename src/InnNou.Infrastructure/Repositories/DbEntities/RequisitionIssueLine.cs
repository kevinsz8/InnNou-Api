namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class RequisitionIssueLine
    {
        public int RequisitionIssueLineId { get; set; }
        public Guid RequisitionIssueLineToken { get; set; }
        public int RequisitionIssueId { get; set; }
        public Guid RequisitionIssueToken { get; set; }

        public int RequisitionLineId { get; set; }
        public Guid RequisitionLineToken { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public int PurchaseUnitId { get; set; }
        public string? PurchaseUnitCode { get; set; }

        public decimal QuantityIssued { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
