SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDERRECTIFICATION - CREATE
   SequenceNumber is computed here (next contiguous per PurchaseOrderId)
   rather than by the caller — PurchaseOrderService already resolves the
   initial @Status before calling this (PENDING_APPROVAL if the
   recompute crossed a new threshold level, APPLIED otherwise; AppliedUtc
   is stamped by the caller in the immediate-apply case via a follow-up
   sp_PurchaseOrderRectification_SetStatus call, not here).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderRectification_Create
(
    @PurchaseOrderRectificationToken UNIQUEIDENTIFIER,
    @PurchaseOrderId                 INT,
    @Reason                          VARCHAR(30),
    @Notes                           NVARCHAR(500) = NULL,
    @Status                          VARCHAR(20),
    @CreatedBy                       VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NextSequenceNumber INT = (SELECT ISNULL(MAX(SequenceNumber), 0) + 1 FROM dbo.PurchaseOrderRectifications WHERE PurchaseOrderId = @PurchaseOrderId);

    INSERT INTO dbo.PurchaseOrderRectifications
        (PurchaseOrderRectificationToken, PurchaseOrderId, SequenceNumber, PurchaseOrderRectificationReasonId, Notes, PurchaseOrderRectificationStatusId, CreatedBy)
    VALUES
        (@PurchaseOrderRectificationToken, @PurchaseOrderId, @NextSequenceNumber,
         (SELECT PurchaseOrderRectificationReasonId FROM dbo.PurchaseOrderRectificationReasons WHERE Code = @Reason),
         @Notes,
         (SELECT PurchaseOrderRectificationStatusId FROM dbo.PurchaseOrderRectificationStatuses WHERE Code = @Status),
         @CreatedBy);

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
    WHERE r.PurchaseOrderRectificationToken = @PurchaseOrderRectificationToken;
END;
GO
