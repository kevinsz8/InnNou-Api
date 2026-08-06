SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_SupplierCreditNoteTaxBreakdown_Create
(
    @SupplierCreditNoteTaxBreakdownToken UNIQUEIDENTIFIER,
    @SupplierCreditNoteId                INT,
    @TaxRatePercent                      DECIMAL(11,8),
    @TaxableAmount                       DECIMAL(18,8),
    @TaxAmount                           DECIMAL(18,8),
    @CurrencyCode                        VARCHAR(10),
    @CreatedBy                           VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SupplierCreditNoteTaxBreakdown
        (SupplierCreditNoteTaxBreakdownToken, SupplierCreditNoteId, TaxRatePercent, TaxableAmount, TaxAmount, CurrencyCode, CreatedBy)
    VALUES
        (@SupplierCreditNoteTaxBreakdownToken, @SupplierCreditNoteId, @TaxRatePercent, @TaxableAmount, @TaxAmount, @CurrencyCode, @CreatedBy);
END;
GO
