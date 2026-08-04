SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDERSHIPMENT - CREATE
   Header for one dispatch event from the source Organization — an
   InternalOrder can have more than one over time (partial stock
   availability today, the rest later). Lines inserted separately via
   sp_InternalOrderShipmentLine_Create, in the same transaction.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrderShipment_Create
(
    @InternalOrderShipmentToken UNIQUEIDENTIFIER,
    @InternalOrderId             INT,
    @SourceWarehouseId           INT,
    @Notes                       NVARCHAR(1000) = NULL,
    @CreatedBy                   VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InternalOrderShipments
        (InternalOrderShipmentToken, InternalOrderId, SourceWarehouseId, Notes, CreatedBy)
    VALUES
        (@InternalOrderShipmentToken, @InternalOrderId, @SourceWarehouseId, @Notes, @CreatedBy);

    SELECT
        ios.InternalOrderShipmentId, ios.InternalOrderShipmentToken,
        ios.InternalOrderId, io.InternalOrderToken, io.InternalOrderNumber,
        ios.SourceWarehouseId, w.WarehouseToken AS SourceWarehouseToken, w.Name AS SourceWarehouseName,
        ios.Notes,
        ios.CreatedUtc, ios.CreatedBy
    FROM dbo.InternalOrderShipments ios
    JOIN dbo.InternalOrders io ON io.InternalOrderId = ios.InternalOrderId
    JOIN dbo.Warehouses w      ON w.WarehouseId      = ios.SourceWarehouseId
    WHERE ios.InternalOrderShipmentToken = @InternalOrderShipmentToken;
END;
GO
