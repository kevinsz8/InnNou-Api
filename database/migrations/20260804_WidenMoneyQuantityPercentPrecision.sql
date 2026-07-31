SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: Widen money/quantity/percent decimal columns to 8 decimals

   Every money and quantity column in this codebase used DECIMAL(18,4);
   every percentage/rate column used DECIMAL(6,3). Both are already exact
   (SQL Server `decimal` is base-10 fixed point, not floating point — this
   was never a rounding-accuracy bug), but the user asked for more headroom
   "porque aca ya estamos jugando con dinero" (2026-08-04), matching the
   precision `UnitConversionRates.Factor` already used (DECIMAL(18,8), the
   one column in this schema that already needed finer-than-money precision
   for conversion-chain math).

   DECIMAL(18,4) -> DECIMAL(18,8): money + quantity columns. Same total
   precision (18), scale moves from 4 to 8 — the integer part shrinks from
   14 to 10 digits, still far beyond any realistic invoice/quantity amount.

   DECIMAL(6,3) -> DECIMAL(11,8): percentage/rate columns. Keeps the same
   3-digit integer-part headroom (values up to 999.xxx, e.g. still supports
   a >100% tolerance override) while widening the fractional part to 8.

   Both directions are lossless, safe widenings — no existing data is
   truncated (SQL Server pads the extra decimal places with zeros).

   Every stored procedure that declares one of these columns as an
   @Parameter must also be widened to match, or SQL Server truncates
   incoming values back down to the old precision regardless of the
   table's own column width — see the sibling stored-procedure updates
   (all `DECIMAL(18,4)`/`DECIMAL(6,3)` parameter declarations across the
   codebase, done in the same pass as this migration).

   Idempotent — each ALTER is guarded by the column's current precision/
   scale, so re-running after the first successful pass is a no-op.
   ============================================================= */

-- ── Money + quantity columns: DECIMAL(18,4) -> DECIMAL(18,8) ──────────────

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('ArticlePackagingLevels') AND c.name = 'QuantityInParentUnit' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE ArticlePackagingLevels ALTER COLUMN QuantityInParentUnit DECIMAL(18,8) NOT NULL;
GO

-- Price is an INCLUDE column on IX_ArticlePrices_Article_Org_Currency_EffectiveDate (a covering
-- index, not a key on Price itself) — SQL Server refuses ALTER COLUMN while any index still
-- references the column, included or not, so drop + recreate around the ALTER.
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('ArticlePrices') AND c.name = 'Price' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
BEGIN
    DROP INDEX IX_ArticlePrices_Article_Org_Currency_EffectiveDate ON ArticlePrices;
    ALTER TABLE ArticlePrices ALTER COLUMN Price DECIMAL(18,8) NOT NULL;
    CREATE INDEX IX_ArticlePrices_Article_Org_Currency_EffectiveDate ON ArticlePrices (ArticleId, OrganizationId, CurrencyCode, EffectiveDate DESC) INCLUDE (Price);
END
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('Articles') AND c.name = 'MinimumOrderQty' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE Articles ALTER COLUMN MinimumOrderQty DECIMAL(18,8) NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('FamilyApprovalThresholds') AND c.name = 'ThresholdAmount' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE FamilyApprovalThresholds ALTER COLUMN ThresholdAmount DECIMAL(18,8) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('GoodsReceiptLine') AND c.name = 'QuantityAccepted' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE GoodsReceiptLine ALTER COLUMN QuantityAccepted DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('GoodsReceiptLine') AND c.name = 'QuantityCourtesy' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE GoodsReceiptLine ALTER COLUMN QuantityCourtesy DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('GoodsReceiptLine') AND c.name = 'QuantityRejected' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE GoodsReceiptLine ALTER COLUMN QuantityRejected DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('GoodsReceiptLine') AND c.name = 'TaxableAmount' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE GoodsReceiptLine ALTER COLUMN TaxableAmount DECIMAL(18,8) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('GoodsReceiptLine') AND c.name = 'TaxAmount' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE GoodsReceiptLine ALTER COLUMN TaxAmount DECIMAL(18,8) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('GoodsReceiptLine') AND c.name = 'TotalAmount' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE GoodsReceiptLine ALTER COLUMN TotalAmount DECIMAL(18,8) NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('InventoryMovements') AND c.name = 'Quantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE InventoryMovements ALTER COLUMN Quantity DECIMAL(18,8) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('InventoryPeriodCounts') AND c.name = 'CountedQuantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE InventoryPeriodCounts ALTER COLUMN CountedQuantity DECIMAL(18,8) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('InventoryPeriodCounts') AND c.name = 'OpeningQuantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE InventoryPeriodCounts ALTER COLUMN OpeningQuantity DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('InventoryPeriodCounts') AND c.name = 'SystemQuantityAtClose' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE InventoryPeriodCounts ALTER COLUMN SystemQuantityAtClose DECIMAL(18,8) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('InventoryPeriodCounts') AND c.name = 'VarianceQuantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE InventoryPeriodCounts ALTER COLUMN VarianceQuantity DECIMAL(18,8) NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('InventoryTransferLines') AND c.name = 'Quantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE InventoryTransferLines ALTER COLUMN Quantity DECIMAL(18,8) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('OrderApprovalSteps') AND c.name = 'ActualFamilyAmount' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE OrderApprovalSteps ALTER COLUMN ActualFamilyAmount DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('OrderApprovalSteps') AND c.name = 'ThresholdAmount' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE OrderApprovalSteps ALTER COLUMN ThresholdAmount DECIMAL(18,8) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('OrderLine') AND c.name = 'ContentQuantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE OrderLine ALTER COLUMN ContentQuantity DECIMAL(18,8) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('OrderLine') AND c.name = 'PurchaseQuantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE OrderLine ALTER COLUMN PurchaseQuantity DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('OrderLine') AND c.name = 'Quantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE OrderLine ALTER COLUMN Quantity DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('OrderLine') AND c.name = 'UnitPrice' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE OrderLine ALTER COLUMN UnitPrice DECIMAL(18,8) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('OrderTemplateLine') AND c.name = 'Quantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE OrderTemplateLine ALTER COLUMN Quantity DECIMAL(18,8) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('ParLevelOverrides') AND c.name = 'MinimumQuantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE ParLevelOverrides ALTER COLUMN MinimumQuantity DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('ParLevelOverrides') AND c.name = 'ReorderQuantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE ParLevelOverrides ALTER COLUMN ReorderQuantity DECIMAL(18,8) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('ParLevels') AND c.name = 'MinimumQuantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE ParLevels ALTER COLUMN MinimumQuantity DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('ParLevels') AND c.name = 'ReorderQuantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE ParLevels ALTER COLUMN ReorderQuantity DECIMAL(18,8) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('PurchaseOrderLine') AND c.name = 'ContentQuantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE PurchaseOrderLine ALTER COLUMN ContentQuantity DECIMAL(18,8) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('PurchaseOrderLine') AND c.name = 'PurchaseQuantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE PurchaseOrderLine ALTER COLUMN PurchaseQuantity DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('PurchaseOrderLine') AND c.name = 'Quantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE PurchaseOrderLine ALTER COLUMN Quantity DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('PurchaseOrderLine') AND c.name = 'UnitPrice' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE PurchaseOrderLine ALTER COLUMN UnitPrice DECIMAL(18,8) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('PurchaseOrderLineRectifications') AND c.name = 'NewQuantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE PurchaseOrderLineRectifications ALTER COLUMN NewQuantity DECIMAL(18,8) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('PurchaseOrderLineRectifications') AND c.name = 'NewUnitPrice' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE PurchaseOrderLineRectifications ALTER COLUMN NewUnitPrice DECIMAL(18,8) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('PurchaseOrderLineRectifications') AND c.name = 'PreviousQuantity' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE PurchaseOrderLineRectifications ALTER COLUMN PreviousQuantity DECIMAL(18,8) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('PurchaseOrderLineRectifications') AND c.name = 'PreviousUnitPrice' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE PurchaseOrderLineRectifications ALTER COLUMN PreviousUnitPrice DECIMAL(18,8) NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('StockLevels') AND c.name = 'QuantityOnHand' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE StockLevels ALTER COLUMN QuantityOnHand DECIMAL(18,8) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('SupplierInvoiceLines') AND c.name = 'QuantityInvoiced' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE SupplierInvoiceLines ALTER COLUMN QuantityInvoiced DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('SupplierInvoiceLines') AND c.name = 'TaxableAmount' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE SupplierInvoiceLines ALTER COLUMN TaxableAmount DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('SupplierInvoiceLines') AND c.name = 'TaxAmount' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE SupplierInvoiceLines ALTER COLUMN TaxAmount DECIMAL(18,8) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('SupplierInvoiceLines') AND c.name = 'TotalAmount' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE SupplierInvoiceLines ALTER COLUMN TotalAmount DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('SupplierInvoiceLines') AND c.name = 'UnitPriceInvoiced' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE SupplierInvoiceLines ALTER COLUMN UnitPriceInvoiced DECIMAL(18,8) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('SupplierInvoiceMatchTolerances') AND c.name = 'ToleranceAmount' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE SupplierInvoiceMatchTolerances ALTER COLUMN ToleranceAmount DECIMAL(18,8) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('SupplierInvoiceTaxBreakdown') AND c.name = 'BaseAmount' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE SupplierInvoiceTaxBreakdown ALTER COLUMN BaseAmount DECIMAL(18,8) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('SupplierInvoiceTaxBreakdown') AND c.name = 'TaxAmount' AND ty.name = 'decimal' AND c.precision = 18 AND c.scale = 4)
    ALTER TABLE SupplierInvoiceTaxBreakdown ALTER COLUMN TaxAmount DECIMAL(18,8) NOT NULL;
GO

-- ── Percentage/rate columns: DECIMAL(6,3) -> DECIMAL(11,8) ─────────────────

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('GoodsReceiptLine') AND c.name = 'TaxRatePercent' AND ty.name = 'decimal' AND c.precision = 6 AND c.scale = 3)
    ALTER TABLE GoodsReceiptLine ALTER COLUMN TaxRatePercent DECIMAL(11,8) NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('SupplierInvoiceLines') AND c.name = 'TaxRatePercent' AND ty.name = 'decimal' AND c.precision = 6 AND c.scale = 3)
    ALTER TABLE SupplierInvoiceLines ALTER COLUMN TaxRatePercent DECIMAL(11,8) NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('SupplierInvoiceMatchTolerances') AND c.name = 'TolerancePercent' AND ty.name = 'decimal' AND c.precision = 6 AND c.scale = 3)
    ALTER TABLE SupplierInvoiceMatchTolerances ALTER COLUMN TolerancePercent DECIMAL(11,8) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('SupplierInvoiceTaxBreakdown') AND c.name = 'TaxRatePercent' AND ty.name = 'decimal' AND c.precision = 6 AND c.scale = 3)
    ALTER TABLE SupplierInvoiceTaxBreakdown ALTER COLUMN TaxRatePercent DECIMAL(11,8) NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID('TaxRates') AND c.name = 'RatePercent' AND ty.name = 'decimal' AND c.precision = 6 AND c.scale = 3)
    ALTER TABLE TaxRates ALTER COLUMN RatePercent DECIMAL(11,8) NOT NULL;
GO

PRINT '=== Migration 20260804_WidenMoneyQuantityPercentPrecision completed successfully ===';
GO
