SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDERSHIPMENT - GET BY INTERNALORDERID
   Header list only (one row per dispatch event) — the caller fetches lines
   separately via sp_InternalOrderShipmentLine_GetByInternalOrderShipmentId
   for a detail view, same "header list, lines on demand" shape as
   GoodsReceipt's own detail assembly.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrderShipment_GetByInternalOrderId
(
    @InternalOrderId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ios.InternalOrderShipmentId, ios.InternalOrderShipmentToken,
        ios.InternalOrderId, io.InternalOrderToken, io.InternalOrderNumber,
        ios.SourceWarehouseId, w.WarehouseToken AS SourceWarehouseToken, w.Name AS SourceWarehouseName,
        ios.Notes,
        ios.CreatedUtc, ios.CreatedBy
    FROM dbo.InternalOrderShipments ios
    JOIN dbo.InternalOrders io ON io.InternalOrderId = ios.InternalOrderId
    JOIN dbo.Warehouses w      ON w.WarehouseId      = ios.SourceWarehouseId
    WHERE ios.InternalOrderId = @InternalOrderId
    ORDER BY ios.CreatedUtc;
END;
GO
