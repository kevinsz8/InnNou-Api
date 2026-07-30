SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICELINE - CREATE
   Single-line insert + re-select, called once per line in a C# loop inside
   SupplierInvoiceService.CreateAsync's shared transaction — same
   one-call-per-line shape as sp_GoodsReceiptLine_Create/
   sp_PurchaseOrderLineRectification_Create. The UNIQUE constraint on
   PurchaseOrderLineId is the real "invoiced at most once" guarantee, not
   this SP.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoiceLine_Create
(
    @SupplierInvoiceLineToken UNIQUEIDENTIFIER,
    @SupplierInvoiceId        INT,
    @PurchaseOrderLineId      INT,
    @ArticleId                INT,
    @QuantityInvoiced         DECIMAL(18,4),
    @UnitPriceInvoiced        DECIMAL(18,4),
    @CurrencyCode             VARCHAR(10),
    @TaxCategoryId            INT           = NULL,
    @TaxRatePercent           DECIMAL(6,3)  = NULL,
    @TaxableAmount            DECIMAL(18,4),
    @TaxAmount                DECIMAL(18,4) = NULL,
    @TotalAmount              DECIMAL(18,4),
    @IsWithinTolerance        BIT,
    @CreatedBy                VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SupplierInvoiceLines
        (SupplierInvoiceLineToken, SupplierInvoiceId, PurchaseOrderLineId, ArticleId,
         QuantityInvoiced, UnitPriceInvoiced, CurrencyCode,
         TaxCategoryId, TaxRatePercent, TaxableAmount, TaxAmount, TotalAmount,
         IsWithinTolerance, CreatedBy)
    VALUES
        (@SupplierInvoiceLineToken, @SupplierInvoiceId, @PurchaseOrderLineId, @ArticleId,
         @QuantityInvoiced, @UnitPriceInvoiced, @CurrencyCode,
         @TaxCategoryId, @TaxRatePercent, @TaxableAmount, @TaxAmount, @TotalAmount,
         @IsWithinTolerance, @CreatedBy);

    SELECT
        sil.SupplierInvoiceLineId, sil.SupplierInvoiceLineToken, sil.SupplierInvoiceId,
        sil.PurchaseOrderLineId, pol.PurchaseOrderLineToken,
        sil.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        sil.QuantityInvoiced, sil.UnitPriceInvoiced, sil.CurrencyCode,
        sil.TaxCategoryId, tc.Code AS TaxCategoryCode, sil.TaxRatePercent,
        sil.TaxableAmount, sil.TaxAmount, sil.TotalAmount,
        sil.IsWithinTolerance,
        sil.CreatedUtc, sil.CreatedBy
    FROM dbo.SupplierInvoiceLines sil
    JOIN dbo.PurchaseOrderLine pol ON pol.PurchaseOrderLineId = sil.PurchaseOrderLineId
    JOIN dbo.Articles a            ON a.ArticleId             = sil.ArticleId
    LEFT JOIN dbo.TaxCategories tc  ON tc.TaxCategoryId        = sil.TaxCategoryId
    WHERE sil.SupplierInvoiceLineToken = @SupplierInvoiceLineToken;
END;
GO
