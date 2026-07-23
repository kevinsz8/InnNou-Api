SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- Full rectification history for a PurchaseOrder, newest first — same "second query, always
-- populated" pattern as sp_OrderLine_GetByOrderId feeding OrderDto.Lines.
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderRectification_GetByPurchaseOrderId
(
    @PurchaseOrderId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.PurchaseOrderRectificationId, r.PurchaseOrderRectificationToken,
        r.PurchaseOrderId, po.PurchaseOrderToken,
        r.SequenceNumber,
        reasons.Code AS Reason,
        r.Notes,
        statuses.Code AS Status,
        r.CreatedUtc, r.CreatedBy, r.AppliedUtc
    FROM dbo.PurchaseOrderRectifications r
    JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = r.PurchaseOrderId
    JOIN dbo.PurchaseOrderRectificationReasons reasons ON reasons.PurchaseOrderRectificationReasonId = r.PurchaseOrderRectificationReasonId
    JOIN dbo.PurchaseOrderRectificationStatuses statuses ON statuses.PurchaseOrderRectificationStatusId = r.PurchaseOrderRectificationStatusId
    WHERE r.PurchaseOrderId = @PurchaseOrderId
    ORDER BY r.SequenceNumber DESC;
END;
GO
