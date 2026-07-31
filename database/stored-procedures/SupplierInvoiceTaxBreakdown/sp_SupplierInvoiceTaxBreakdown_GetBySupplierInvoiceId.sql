SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICETAXBREAKDOWN - GET BY SUPPLIERINVOICE ID
   Tax-rate breakdown rows for a single SupplierInvoice — populates
   SupplierInvoiceDto.TaxBreakdown.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoiceTaxBreakdown_GetBySupplierInvoiceId
(
    @SupplierInvoiceId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sitb.SupplierInvoiceTaxBreakdownId, sitb.SupplierInvoiceTaxBreakdownToken, sitb.SupplierInvoiceId,
        sitb.TaxRatePercent, sitb.BaseAmount, sitb.TaxAmount,
        sitb.CreatedUtc, sitb.CreatedBy
    FROM dbo.SupplierInvoiceTaxBreakdown sitb
    WHERE sitb.SupplierInvoiceId = @SupplierInvoiceId
    ORDER BY sitb.TaxRatePercent;
END;
GO
