SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITION - APPROVE
   Only reachable from REQUESTED. The WHERE clause is the real gate
   (defense-in-depth, mirrors every other status-guarded UPDATE in this
   codebase); the service's own status check is the primary, user-facing one.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Requisition_Approve
(
    @RequisitionToken UNIQUEIDENTIFIER,
    @ApprovedBy       VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Requisitions
    SET RequisitionStatusId = (SELECT RequisitionStatusId FROM dbo.RequisitionStatuses WHERE Code = 'APPROVED'),
        ApprovedUtc = SYSUTCDATETIME(),
        ApprovedBy = @ApprovedBy,
        LastUpdatedUtc = SYSUTCDATETIME(),
        LastUpdatedBy = @ApprovedBy
    WHERE RequisitionToken = @RequisitionToken
      AND RequisitionStatusId = (SELECT RequisitionStatusId FROM dbo.RequisitionStatuses WHERE Code = 'REQUESTED');

    SELECT
        r.RequisitionId, r.RequisitionToken, r.RequisitionNumber,
        r.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        r.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        r.DepartmentId, d.DepartmentToken, d.Name AS DepartmentName,
        rs.Code AS Status,
        r.Notes,
        r.ApprovedUtc, r.ApprovedBy,
        r.RejectedUtc, r.RejectedBy, r.RejectedReason,
        r.CancelledUtc, r.CancelledBy, r.CancelledReason,
        r.ClosedShortUtc, r.ClosedShortBy, r.ClosedShortReason,
        r.CreatedUtc, r.CreatedBy, r.LastUpdatedUtc, r.LastUpdatedBy
    FROM dbo.Requisitions r
    JOIN dbo.Organizations org ON org.OrganizationId = r.OrganizationId
    JOIN dbo.Warehouses w      ON w.WarehouseId      = r.WarehouseId
    JOIN dbo.Departments d     ON d.DepartmentId     = r.DepartmentId
    JOIN dbo.RequisitionStatuses rs ON rs.RequisitionStatusId = r.RequisitionStatusId
    WHERE r.RequisitionToken = @RequisitionToken;
END;
GO
