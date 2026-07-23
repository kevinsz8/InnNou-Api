SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDERRECTIFICATION - SET STATUS
   Generic transition, used for both PENDING_APPROVAL -> APPLIED (once
   every triggered OrderApprovalStep is APPROVED) and
   PENDING_APPROVAL -> REJECTED. AppliedUtc is only stamped on the
   APPLIED transition. Also used directly from
   PurchaseOrderService.CreateRectificationAsync to stamp APPLIED
   immediately when no new approval level was crossed.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderRectification_SetStatus
(
    @PurchaseOrderRectificationId INT,
    @Status                       VARCHAR(20)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PurchaseOrderRectifications
    SET    PurchaseOrderRectificationStatusId = (SELECT PurchaseOrderRectificationStatusId FROM dbo.PurchaseOrderRectificationStatuses WHERE Code = @Status),
           AppliedUtc = CASE WHEN @Status = 'APPLIED' THEN SYSUTCDATETIME() ELSE AppliedUtc END
    WHERE  PurchaseOrderRectificationId = @PurchaseOrderRectificationId;

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
    WHERE r.PurchaseOrderRectificationId = @PurchaseOrderRectificationId;
END;
GO
