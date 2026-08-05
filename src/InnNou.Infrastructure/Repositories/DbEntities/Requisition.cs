using InnNou.Application.Common;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Requisition
    {
        public int RequisitionId { get; set; }
        public Guid RequisitionToken { get; set; }
        public string RequisitionNumber { get; set; } = default!;

        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }

        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }

        public int DepartmentId { get; set; }
        public Guid DepartmentToken { get; set; }
        public string? DepartmentName { get; set; }

        public RequisitionStatus Status { get; set; }
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

        // Only populated by sp_Requisition_GetPaged (a cheap CROSS APPLY COUNT, same convention as
        // PurchaseOrder/InternalOrder's own LineCount).
        public int LineCount { get; set; }
    }
}
