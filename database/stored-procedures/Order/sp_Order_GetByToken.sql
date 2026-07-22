SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDER - GET BY TOKEN
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Order_GetByToken
(
    @OrderToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.OrderId, o.OrderToken, o.OrganizationId, org.OrganizationToken,
        o.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        os.Code AS Status, o.Notes, o.SubmittedUtc,
        o.CreatedUtc, o.CreatedBy, o.LastUpdatedUtc, o.LastUpdatedBy
    FROM dbo.[Order] o
    JOIN dbo.Organizations org ON org.OrganizationId = o.OrganizationId
    JOIN dbo.Warehouses    w   ON w.WarehouseId      = o.WarehouseId
    JOIN dbo.OrderStatuses os  ON os.OrderStatusId    = o.OrderStatusId
    WHERE o.OrderToken = @OrderToken;
END;
GO
