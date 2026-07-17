SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDER - DELETE
   Hard delete, DRAFT only — a Draft cart was never sent anywhere, no
   history worth protecting. Re-checks Status = 'DRAFT' in the WHERE
   itself (defense in depth), independent of the service-layer check.
   Lines are removed first since FK_OrderLine_Order has no cascade.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Order_Delete
(
    @OrderToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OrderId INT = (SELECT OrderId FROM dbo.[Order] WHERE OrderToken = @OrderToken AND Status = 'DRAFT');

    IF @OrderId IS NULL
        RETURN;

    DELETE FROM dbo.OrderLine WHERE OrderId = @OrderId;
    DELETE FROM dbo.[Order]   WHERE OrderId = @OrderId;
END;
GO
