SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDER - SET STATUS
   Generic status transition (REQUESTED -> SHIPPED -> PARTIALLY_RECEIVED ->
   RECEIVED). sp_InternalOrder_Cancel remains the dedicated REQUESTED ->
   CANCELLED transition (stamps CancelledUtc/By/Reason, which this one
   deliberately doesn't touch). The "is this fully received" decision is
   computed in C#, not here — same shape as sp_PurchaseOrder_SetStatus.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrder_SetStatus
(
    @InternalOrderToken UNIQUEIDENTIFIER,
    @Status              VARCHAR(20)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.InternalOrders
    SET InternalOrderStatusId = (SELECT InternalOrderStatusId FROM dbo.InternalOrderStatuses WHERE Code = @Status),
        LastUpdatedUtc = SYSUTCDATETIME()
    WHERE InternalOrderToken = @InternalOrderToken;

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
