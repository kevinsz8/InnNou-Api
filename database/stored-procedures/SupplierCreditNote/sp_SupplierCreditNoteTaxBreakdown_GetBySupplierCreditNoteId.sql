SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_SupplierCreditNoteTaxBreakdown_GetBySupplierCreditNoteId
(
    @SupplierCreditNoteId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SupplierCreditNoteTaxBreakdownToken, TaxRatePercent, TaxableAmount, TaxAmount, CurrencyCode
    FROM dbo.SupplierCreditNoteTaxBreakdown
    WHERE SupplierCreditNoteId = @SupplierCreditNoteId
    ORDER BY TaxRatePercent;
END;
GO
