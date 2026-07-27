SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYPERIOD - SET STATUS
   Generic status transition covering all four edges of the state machine
   (OPEN/IN_PROGRESS/PRE_CLOSED auto-computed by InventoryPeriodService.
   SubmitCountAsync from count completeness; CLOSED/back-to-PRE_CLOSED are
   the explicit Close/Reopen actions) — same "resolve Code->Id inline, the
   actual transition decision is made in C#" shape as
   sp_PurchaseOrder_SetStatus.

   @ClosedUtc/@ClosedBy are set (via COALESCE, so omitting them on a plain
   IN_PROGRESS/PRE_CLOSED auto-transition leaves any prior value untouched)
   only by CloseAsync. @ReopenedUtc/@ReopenedBy likewise only by ReopenAsync.
   @ClearClosedFields=1 is passed only by ReopenAsync, to null out
   ClosedUtc/ClosedBy since the period is no longer closed.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryPeriod_SetStatus
(
    @InventoryPeriodToken UNIQUEIDENTIFIER,
    @Status               VARCHAR(20),
    @ActorBy              VARCHAR(150),
    @ClosedUtc            DATETIME2      = NULL,
    @ClosedBy             VARCHAR(150)   = NULL,
    @ReopenedUtc          DATETIME2      = NULL,
    @ReopenedBy           VARCHAR(150)   = NULL,
    @ClearClosedFields    BIT            = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.InventoryPeriods
    SET InventoryPeriodStatusId = (SELECT InventoryPeriodStatusId FROM dbo.InventoryPeriodStatuses WHERE Code = @Status),
        ClosedUtc      = CASE WHEN @ClearClosedFields = 1 THEN NULL ELSE COALESCE(@ClosedUtc, ClosedUtc) END,
        ClosedBy       = CASE WHEN @ClearClosedFields = 1 THEN NULL ELSE COALESCE(@ClosedBy, ClosedBy) END,
        ReopenedUtc    = COALESCE(@ReopenedUtc, ReopenedUtc),
        ReopenedBy     = COALESCE(@ReopenedBy, ReopenedBy),
        LastUpdatedUtc = SYSUTCDATETIME(),
        LastUpdatedBy  = @ActorBy
    WHERE InventoryPeriodToken = @InventoryPeriodToken;

    SELECT
        ip.InventoryPeriodId, ip.InventoryPeriodToken,
        ip.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.OrganizationId,
        ips.Code AS Status,
        ip.StartDate, ip.ClosedUtc, ip.ClosedBy, ip.ReopenedUtc, ip.ReopenedBy, ip.Notes,
        ip.CreatedUtc, ip.CreatedBy, ip.LastUpdatedUtc, ip.LastUpdatedBy
    FROM dbo.InventoryPeriods ip
    JOIN dbo.Warehouses w              ON w.WarehouseId              = ip.WarehouseId
    JOIN dbo.InventoryPeriodStatuses ips ON ips.InventoryPeriodStatusId = ip.InventoryPeriodStatusId
    WHERE ip.InventoryPeriodToken = @InventoryPeriodToken;
END;
GO
