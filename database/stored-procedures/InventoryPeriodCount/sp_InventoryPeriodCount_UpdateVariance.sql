SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYPERIODCOUNT - UPDATE VARIANCE
   Sets SystemQuantityAtClose/VarianceQuantity for one line — called by
   InventoryPeriodService.CloseAsync with the live-computed values, and by
   ReopenAsync with both NULL (clearing them back to "unposted") since a
   reopened period returns to PRE_CLOSED with its CountedQuantity preserved
   but its close-time snapshot undone. No re-select — the caller re-hydrates
   every line at once afterwards via sp_InventoryPeriodCount_GetByPeriodId.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryPeriodCount_UpdateVariance
(
    @InventoryPeriodCountId INT,
    @SystemQuantityAtClose  DECIMAL(18,4) = NULL,
    @VarianceQuantity       DECIMAL(18,4) = NULL,
    @ActorBy                VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.InventoryPeriodCounts
    SET SystemQuantityAtClose = @SystemQuantityAtClose,
        VarianceQuantity = @VarianceQuantity,
        LastUpdatedUtc = SYSUTCDATETIME(),
        LastUpdatedBy = @ActorBy
    WHERE InventoryPeriodCountId = @InventoryPeriodCountId;
END;
GO
