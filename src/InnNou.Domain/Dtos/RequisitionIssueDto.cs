namespace InnNou.Domain.Dtos
{
    public class RequisitionIssueDto
    {
        public Guid RequisitionIssueToken { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        public List<RequisitionIssueLineDto> Lines { get; set; } = [];
    }
}
