SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASE ORDER RECTIFICATIONS - ALLOW ADDING NEW LINES
   Extends rectifications beyond quantity/price changes and
   cancellations to also support adding a line for an article that
   was never on the original PurchaseOrder (e.g. the supplier shipped
   something ordered by phone that never made it onto the formal PO).
   A rectification-added PurchaseOrderLine has no originating OrderLine
   (it was never part of the cart Order's split at Submit time), so
   OrderLineId must become nullable — the existing plain unique index
   on it must become filtered first, since SQL Server's default unique
   index only tolerates a single NULL across the whole column.
   ============================================================= */

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PurchaseOrderLine_OrderLineId' AND object_id = OBJECT_ID('PurchaseOrderLine'))
BEGIN
    DROP INDEX UX_PurchaseOrderLine_OrderLineId ON PurchaseOrderLine;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PurchaseOrderLine') AND name = 'OrderLineId' AND is_nullable = 0)
BEGIN
    ALTER TABLE dbo.PurchaseOrderLine ALTER COLUMN OrderLineId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PurchaseOrderLine_OrderLineId' AND object_id = OBJECT_ID('PurchaseOrderLine'))
BEGIN
    CREATE UNIQUE INDEX UX_PurchaseOrderLine_OrderLineId ON PurchaseOrderLine (OrderLineId) WHERE OrderLineId IS NOT NULL;
END
GO

-- Seed order matters — the C# PurchaseOrderRectificationLineAction enum hardcodes this Id (3),
-- appended after the existing QUANTITY_PRICE_CHANGE=1/LINE_CANCELLED=2 rows.
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderRectificationLineActions WHERE Code = 'LINE_ADDED')
    INSERT INTO PurchaseOrderRectificationLineActions (Code) VALUES ('LINE_ADDED');
GO
