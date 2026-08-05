namespace InnNou.Application.Responses.Common
{
    public class RequisitionIssue
    {
        public Guid RequisitionIssueToken { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        public List<RequisitionIssueLine> Lines { get; set; } = [];
    }
}
