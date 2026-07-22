SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDER - SET STATUS
   Generic transition, used for both DRAFT -> SUBMITTED (inside the
   split transaction, see sp_PurchaseOrder_Create/sp_OrderLine_SetPurchaseOrder)
   and DRAFT -> CANCELLED. SubmittedUtc is only stamped on the
   SUBMITTED transition.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Order_SetStatus
(
    @OrderToken UNIQUEIDENTIFIER,
    @Status     VARCHAR(20),
    @ActorBy    VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.[Order] WHERE OrderToken = @OrderToken)
    BEGIN
        RAISERROR('ORDER_NOT_FOUND', 16, 1);
        RETURN;
    END

    DECLARE @NewStatusId INT = (SELECT OrderStatusId FROM dbo.OrderStatuses WHERE Code = @Status);

    UPDATE dbo.[Order]
    SET
        OrderStatusId  = @NewStatusId,
        SubmittedUtc   = CASE WHEN @Status = 'SUBMITTED' THEN SYSUTCDATETIME() ELSE SubmittedUtc END,
        LastUpdatedUtc = SYSUTCDATETIME(),
        LastUpdatedBy  = @ActorBy
    WHERE OrderToken = @OrderToken;

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
