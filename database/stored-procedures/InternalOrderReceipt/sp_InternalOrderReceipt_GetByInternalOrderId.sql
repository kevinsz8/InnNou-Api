SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDERRECEIPT - GET BY INTERNALORDERID
   Header list only (one row per receiving event) — the caller fetches lines
   separately via sp_InternalOrderReceiptLine_GetByInternalOrderReceiptId for
   a detail view, same "header list, lines on demand" shape as
   InternalOrderShipment's own detail assembly.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrderReceipt_GetByInternalOrderId
(
    @InternalOrderId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ior.InternalOrderReceiptId, ior.InternalOrderReceiptToken,
        ior.InternalOrderId, io.InternalOrderToken, io.InternalOrderNumber,
        ior.Notes,
        ior.CreatedUtc, ior.CreatedBy
    FROM dbo.InternalOrderReceipts ior
    JOIN dbo.InternalOrders io ON io.InternalOrderId = ior.InternalOrderId
    WHERE ior.InternalOrderId = @InternalOrderId
    ORDER BY ior.CreatedUtc;
END;
GO
