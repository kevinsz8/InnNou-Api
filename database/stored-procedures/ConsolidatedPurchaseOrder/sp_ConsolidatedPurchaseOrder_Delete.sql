SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- Hard delete — a ConsolidatedPurchaseOrder has no downstream side effects (unlike Order/
-- PurchaseOrder, it never triggers a real business process), so removing a mistaken one is
-- safe. Members cascade-delete via FK ON DELETE CASCADE.
CREATE OR ALTER PROCEDURE dbo.sp_ConsolidatedPurchaseOrder_Delete
(
    @ConsolidatedPurchaseOrderToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.ConsolidatedPurchaseOrders WHERE ConsolidatedPurchaseOrderToken = @ConsolidatedPurchaseOrderToken;
END;
GO
