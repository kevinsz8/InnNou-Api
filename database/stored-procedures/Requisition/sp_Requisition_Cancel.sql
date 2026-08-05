SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITION - CANCEL
   Reachable from REQUESTED or APPROVED — once anything has actually been
   issued (PARTIALLY_ISSUED), the remaining balance is closed out via
   sp_Requisition_CloseShort instead, which preserves the fact that some
   stock already left the store (same CANCELLED-vs-CLOSED_SHORT split as
   PurchaseOrder's own).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Requisition_Cancel
(
    @RequisitionToken UNIQUEIDENTIFIER,
    @CancelledBy       VARCHAR(150),
    @CancelledReason    NVARCHAR(500) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Requisitions
    SET RequisitionStatusId = (SELECT RequisitionStatusId FROM dbo.RequisitionStatuses WHERE Code = 'CANCELLED'),
        CancelledUtc = SYSUTCDATETIME(),
        CancelledBy = @CancelledBy,
        CancelledReason = @CancelledReason,
        LastUpdatedUtc = SYSUTCDATETIME(),
        LastUpdatedBy = @CancelledBy
    WHERE RequisitionToken = @RequisitionToken
      AND RequisitionStatusId IN (
          SELECT RequisitionStatusId FROM dbo.RequisitionStatuses WHERE Code IN ('REQUESTED', 'APPROVED')
      );

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
