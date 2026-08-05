SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITION - REJECT
   Only reachable from REQUESTED (same as Approve — a decision on the
   original request, before anything was committed to be issued).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Requisition_Reject
(
    @RequisitionToken UNIQUEIDENTIFIER,
    @RejectedBy       VARCHAR(150),
    @RejectedReason    NVARCHAR(500)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Requisitions
    SET RequisitionStatusId = (SELECT RequisitionStatusId FROM dbo.RequisitionStatuses WHERE Code = 'REJECTED'),
        RejectedUtc = SYSUTCDATETIME(),
        RejectedBy = @RejectedBy,
        RejectedReason = @RejectedReason,
        LastUpdatedUtc = SYSUTCDATETIME(),
        LastUpdatedBy = @RejectedBy
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
