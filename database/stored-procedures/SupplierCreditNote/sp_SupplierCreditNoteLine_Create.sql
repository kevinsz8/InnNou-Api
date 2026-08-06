SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_SupplierCreditNoteLine_Create
(
    @SupplierCreditNoteLineToken UNIQUEIDENTIFIER,
    @SupplierCreditNoteId        INT,
    @SupplierReturnLineId        INT,
    @ArticleId                   INT,
    @QuantityCredited            DECIMAL(18,8),
    @UnitPrice                   DECIMAL(18,8),
    @CurrencyCode                VARCHAR(10),
    @TaxCategoryId               INT = NULL,
    @TaxRatePercent              DECIMAL(11,8) = NULL,
    @TaxableAmount               DECIMAL(18,8),
    @TaxAmount                   DECIMAL(18,8),
    @TotalAmount                 DECIMAL(18,8),
    @WasManuallyEntered          BIT = 0,
    @CreatedBy                   VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SupplierCreditNoteLines
        (SupplierCreditNoteLineToken, SupplierCreditNoteId, SupplierReturnLineId, ArticleId,
         QuantityCredited, UnitPrice, CurrencyCode, TaxCategoryId, TaxRatePercent,
         TaxableAmount, TaxAmount, TotalAmount, WasManuallyEntered, CreatedBy)
    VALUES
        (@SupplierCreditNoteLineToken, @SupplierCreditNoteId, @SupplierReturnLineId, @ArticleId,
         @QuantityCredited, @UnitPrice, @CurrencyCode, @TaxCategoryId, @TaxRatePercent,
         @TaxableAmount, @TaxAmount, @TotalAmount, @WasManuallyEntered, @CreatedBy);

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
    WHERE scnl.SupplierCreditNoteLineToken = @SupplierCreditNoteLineToken;
END;
GO
