SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICETAXBREAKDOWN - CREATE
   Single-row insert, called once per tax-rate row in a C# loop inside
   SupplierInvoiceService.CreateAsync's shared transaction — same
   one-call-per-row shape as sp_SupplierInvoiceLine_Create. TaxAmount is
   computed by the caller (server-side, from BaseAmount * TaxRatePercent),
   never trusted from the client.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoiceTaxBreakdown_Create
(
    @SupplierInvoiceTaxBreakdownToken UNIQUEIDENTIFIER,
    @SupplierInvoiceId                INT,
    @TaxRatePercent                   DECIMAL(11,8)  = NULL,
    @BaseAmount                       DECIMAL(18,8),
    @TaxAmount                        DECIMAL(18,8),
    @CreatedBy                        VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SupplierInvoiceTaxBreakdown
        (SupplierInvoiceTaxBreakdownToken, SupplierInvoiceId, TaxRatePercent, BaseAmount, TaxAmount, CreatedBy)
    VALUES
        (@SupplierInvoiceTaxBreakdownToken, @SupplierInvoiceId, @TaxRatePercent, @BaseAmount, @TaxAmount, @CreatedBy);
END;
GO
