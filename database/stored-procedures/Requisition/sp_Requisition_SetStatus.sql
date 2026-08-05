SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITION - SET STATUS
   Generic status transition for issuance (APPROVED -> PARTIALLY_ISSUED ->
   ISSUED) — sp_Requisition_Cancel/Reject/CloseShort remain the dedicated
   transitions that also stamp their own actor/reason fields, which this one
   deliberately doesn't touch. Called by RequisitionService.CreateIssueAsync
   inside the same transaction as the RequisitionIssue/RequisitionIssueLine
   inserts — the "is this fully issued yet" decision itself is computed in
   C#, not here, same shape as sp_PurchaseOrder_SetStatus.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Requisition_SetStatus
(
    @RequisitionToken UNIQUEIDENTIFIER,
    @Status            VARCHAR(20)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Requisitions
    SET RequisitionStatusId = (SELECT RequisitionStatusId FROM dbo.RequisitionStatuses WHERE Code = @Status),
        LastUpdatedUtc = SYSUTCDATETIME()
    WHERE RequisitionToken = @RequisitionToken;

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
