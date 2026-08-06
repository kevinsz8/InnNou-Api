SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_SupplierCreditNoteLine_GetBySupplierCreditNoteId
(
    @SupplierCreditNoteId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        scnl.SupplierCreditNoteLineId, scnl.SupplierCreditNoteLineToken, scnl.SupplierCreditNoteId,
        scnl.SupplierReturnLineId, srl.SupplierReturnLineToken,
        scnl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        scnl.QuantityCredited, scnl.UnitPrice, scnl.CurrencyCode,
        scnl.TaxCategoryId, tc.Code AS TaxCategoryCode, scnl.TaxRatePercent,
        scnl.TaxableAmount, scnl.TaxAmount, scnl.TotalAmount, scnl.WasManuallyEntered,
        scnl.CreatedUtc, scnl.CreatedBy
    FROM dbo.SupplierCreditNoteLines scnl
    JOIN dbo.SupplierReturnLines srl ON srl.SupplierReturnLineId = scnl.SupplierReturnLineId
    JOIN dbo.Articles a              ON a.ArticleId              = scnl.ArticleId
    LEFT JOIN dbo.TaxCategories tc   ON tc.TaxCategoryId         = scnl.TaxCategoryId
    WHERE scnl.SupplierCreditNoteId = @SupplierCreditNoteId
    ORDER BY scnl.SupplierCreditNoteLineId;
END;
GO
