SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   GOODSRECEIPTLINE - CREATE
   Single-line insert + re-select, called once per line in a C# loop inside
   PurchaseOrderService.CreateGoodsReceiptAsync's shared transaction — same
   one-call-per-line shape as sp_PurchaseOrderLineRectification_Create (no
   TVP/JSON batch parameter exists anywhere in this codebase).

   The 3-way quantity split (Accepted/Courtesy/Rejected) is validated by the
   caller before this call — QuantityAccepted capped against the line's
   remaining-to-receive, Courtesy/Rejected uncapped by design. The DB-layer
   CHECK constraints (non-negative, not-all-zero) are the backstop, not the
   primary gate.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_GoodsReceiptLine_Create
(
    @GoodsReceiptLineToken UNIQUEIDENTIFIER,
    @GoodsReceiptId        INT,
    @PurchaseOrderLineId   INT,
    @ArticleId             INT,
    @QuantityAccepted      DECIMAL(18,4) = 0,
    @QuantityCourtesy      DECIMAL(18,4) = 0,
    @QuantityRejected      DECIMAL(18,4) = 0,
    @RejectionReason       NVARCHAR(500) = NULL,
    @LotNumber             NVARCHAR(100) = NULL,
    @ExpirationDate        DATE          = NULL,
    @SerialNumber          NVARCHAR(100) = NULL,
    @Notes                 NVARCHAR(500) = NULL,
    @CreatedBy             VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.GoodsReceiptLine
        (GoodsReceiptLineToken, GoodsReceiptId, PurchaseOrderLineId, ArticleId,
         QuantityAccepted, QuantityCourtesy, QuantityRejected, RejectionReason,
         LotNumber, ExpirationDate, SerialNumber, Notes, CreatedBy)
    VALUES
        (@GoodsReceiptLineToken, @GoodsReceiptId, @PurchaseOrderLineId, @ArticleId,
         @QuantityAccepted, @QuantityCourtesy, @QuantityRejected, @RejectionReason,
         @LotNumber, @ExpirationDate, @SerialNumber, @Notes, @CreatedBy);

    SELECT
        grl.GoodsReceiptLineId, grl.GoodsReceiptLineToken, grl.GoodsReceiptId,
        grl.PurchaseOrderLineId, pol.PurchaseOrderLineToken, pol.Quantity AS OrderedQuantity,
        grl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        grl.QuantityAccepted, grl.QuantityCourtesy, grl.QuantityRejected, grl.RejectionReason,
        grl.LotNumber, grl.ExpirationDate, grl.SerialNumber, grl.Notes,
        grl.CreatedUtc, grl.CreatedBy
    FROM dbo.GoodsReceiptLine grl
    JOIN dbo.PurchaseOrderLine pol ON pol.PurchaseOrderLineId = grl.PurchaseOrderLineId
    JOIN dbo.Articles a            ON a.ArticleId             = grl.ArticleId
    WHERE grl.GoodsReceiptLineToken = @GoodsReceiptLineToken;
END;
GO
