SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDER - CREATE
   Creates a new cart in DRAFT status against a resolved Warehouse.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Order_Create
(
    @OrderToken     UNIQUEIDENTIFIER,
    @OrganizationId INT,
    @WarehouseId    INT,
    @Notes          NVARCHAR(500) = NULL,
    @CreatedBy      VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.[Order] (OrderToken, OrganizationId, WarehouseId, OrderStatusId, Notes, CreatedBy)
    VALUES (@OrderToken, @OrganizationId, @WarehouseId, (SELECT OrderStatusId FROM dbo.OrderStatuses WHERE Code = 'DRAFT'), @Notes, @CreatedBy);

    SELECT
        o.OrderId, o.OrderToken, o.OrganizationId, org.OrganizationToken,
        o.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        os.Code AS Status, o.Notes, o.SubmittedUtc, o.PdfUrl,
        o.CreatedUtc, o.CreatedBy, o.LastUpdatedUtc, o.LastUpdatedBy
    FROM dbo.[Order] o
    JOIN dbo.Organizations org ON org.OrganizationId = o.OrganizationId
    JOIN dbo.Warehouses    w   ON w.WarehouseId      = o.WarehouseId
    JOIN dbo.OrderStatuses os  ON os.OrderStatusId    = o.OrderStatusId
    WHERE o.OrderToken = @OrderToken;
END;
GO
