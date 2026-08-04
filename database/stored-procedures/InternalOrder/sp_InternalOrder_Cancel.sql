SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDER - CANCEL
   Only reachable from REQUESTED — once the source Organization has shipped
   anything, the order is physically in motion and can no longer be
   cancelled outright (same "can't un-ring the bell" principle as
   sp_PurchaseOrder_Cancel's own SENT-only guard). The WHERE clause is the
   real gate (defense-in-depth, mirrors every other status-guarded UPDATE in
   this codebase); the service's own status check is the primary,
   user-facing one.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrder_Cancel
(
    @InternalOrderToken UNIQUEIDENTIFIER,
    @CancelledBy         VARCHAR(150),
    @CancelledReason      NVARCHAR(500)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.InternalOrders
    SET InternalOrderStatusId = (SELECT InternalOrderStatusId FROM dbo.InternalOrderStatuses WHERE Code = 'CANCELLED'),
        CancelledUtc = SYSUTCDATETIME(),
        CancelledBy = @CancelledBy,
        CancelledReason = @CancelledReason,
        LastUpdatedUtc = SYSUTCDATETIME(),
        LastUpdatedBy = @CancelledBy
    WHERE InternalOrderToken = @InternalOrderToken
      AND InternalOrderStatusId = (SELECT InternalOrderStatusId FROM dbo.InternalOrderStatuses WHERE Code = 'REQUESTED');

    SELECT
        io.InternalOrderId, io.InternalOrderToken, io.InternalOrderNumber,
        io.RequestingOrganizationId, reqOrg.OrganizationToken AS RequestingOrganizationToken, reqOrg.Name AS RequestingOrganizationName,
        io.SourceOrganizationId, srcOrg.OrganizationToken AS SourceOrganizationToken, srcOrg.Name AS SourceOrganizationName,
        io.DestinationWarehouseId, dw.WarehouseToken AS DestinationWarehouseToken, dw.Name AS DestinationWarehouseName,
        ios.Code AS Status,
        io.Notes,
        io.CancelledUtc, io.CancelledBy, io.CancelledReason,
        io.CreatedUtc, io.CreatedBy, io.LastUpdatedUtc, io.LastUpdatedBy
    FROM dbo.InternalOrders io
    JOIN dbo.Organizations reqOrg ON reqOrg.OrganizationId = io.RequestingOrganizationId
    JOIN dbo.Organizations srcOrg ON srcOrg.OrganizationId = io.SourceOrganizationId
    JOIN dbo.Warehouses dw ON dw.WarehouseId = io.DestinationWarehouseId
    JOIN dbo.InternalOrderStatuses ios ON ios.InternalOrderStatusId = io.InternalOrderStatusId
    WHERE io.InternalOrderToken = @InternalOrderToken;
END;
GO
