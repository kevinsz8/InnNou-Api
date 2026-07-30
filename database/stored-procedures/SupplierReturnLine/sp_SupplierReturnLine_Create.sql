SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERRETURNLINE - CREATE
   The UQ_SupplierReturnLines_GoodsReceiptLineId unique constraint is the
   real guard against claiming the same rejected line twice — the service
   layer also checks first (for a clean error message), but the constraint
   is what actually prevents a race between two concurrent requests.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierReturnLine_Create
(
    @SupplierReturnLineToken UNIQUEIDENTIFIER,
    @SupplierReturnId        INT,
    @GoodsReceiptLineId      INT,
    @Notes                   NVARCHAR(500) = NULL,
    @CreatedBy               VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SupplierReturnLines
        (SupplierReturnLineToken, SupplierReturnId, GoodsReceiptLineId, Notes, CreatedBy)
    VALUES
        (@SupplierReturnLineToken, @SupplierReturnId, @GoodsReceiptLineId, @Notes, @CreatedBy);

    SELECT
        srl.SupplierReturnLineId, srl.SupplierReturnLineToken,
        srl.SupplierReturnId,
        srl.GoodsReceiptLineId, grl.GoodsReceiptLineToken,
        grl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        grl.QuantityRejected, grl.RejectionReason,
        srl.Notes, srl.CreatedUtc, srl.CreatedBy
    FROM dbo.SupplierReturnLines srl
    JOIN dbo.GoodsReceiptLine grl ON grl.GoodsReceiptLineId = srl.GoodsReceiptLineId
    JOIN dbo.Articles a ON a.ArticleId = grl.ArticleId
    WHERE srl.SupplierReturnLineToken = @SupplierReturnLineToken;
END;
GO
