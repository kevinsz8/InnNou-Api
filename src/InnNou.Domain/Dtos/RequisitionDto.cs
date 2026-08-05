namespace InnNou.Domain.Dtos
{
    public class RequisitionDto
    {
        public Guid RequisitionToken { get; set; }
        public string RequisitionNumber { get; set; } = default!;

        public Guid OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }

        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }

        public Guid DepartmentToken { get; set; }
        public string? DepartmentName { get; set; }

        public string Status { get; set; } = default!;
        public string? Notes { get; set; }

        public DateTime? ApprovedUtc { get; set; }
        public string? ApprovedBy { get; set; }

        public DateTime? RejectedUtc { get; set; }
        public string? RejectedBy { get; set; }
        public string? RejectedReason { get; set; }

        public DateTime? CancelledUtc { get; set; }
        public string? CancelledBy { get; set; }
        public string? CancelledReason { get; set; }

        public DateTime? ClosedShortUtc { get; set; }
        public string? ClosedShortBy { get; set; }
        public string? ClosedShortReason { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }

        public int LineCount { get; set; }

        // Populated by RequisitionService.GetByTokenAsync only — GetPagedAsync leaves these empty
        // and relies on LineCount, same "header list vs. full detail" split as InternalOrder.
        public List<RequisitionLineDto> Lines { get; set; } = [];
        public List<RequisitionIssueDto> Issues { get; set; } = [];
    }
}
