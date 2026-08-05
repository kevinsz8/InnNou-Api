namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class RequisitionIssue
    {
        public int RequisitionIssueId { get; set; }
        public Guid RequisitionIssueToken { get; set; }
        public int RequisitionId { get; set; }
        public Guid RequisitionToken { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
