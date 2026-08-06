SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_SupplierReturnLine_GetBySupplierReturnId
(
    @SupplierReturnId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        srl.SupplierReturnLineId, srl.SupplierReturnLineToken,
        srl.SupplierReturnId,
        srl.GoodsReceiptLineId, grl.GoodsReceiptLineToken, grl.GoodsReceiptId,
        grl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        grl.QuantityRejected, grl.RejectionReason,
        srl.Notes, srl.CreatedUtc, srl.CreatedBy,
        grl.UnitPrice, grl.CurrencyCode, grl.TaxCategoryId, grl.TaxRatePercent
    FROM dbo.SupplierReturnLines srl
    JOIN dbo.GoodsReceiptLine grl ON grl.GoodsReceiptLineId = srl.GoodsReceiptLineId
    JOIN dbo.Articles a ON a.ArticleId = grl.ArticleId
    WHERE srl.SupplierReturnId = @SupplierReturnId
    ORDER BY srl.SupplierReturnLineId;
END;
GO
