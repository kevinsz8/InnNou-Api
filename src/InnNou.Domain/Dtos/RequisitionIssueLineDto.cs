namespace InnNou.Domain.Dtos
{
    public class RequisitionIssueLineDto
    {
        public Guid RequisitionIssueLineToken { get; set; }
        public Guid RequisitionLineToken { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? PurchaseUnitCode { get; set; }

        public decimal QuantityIssued { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
