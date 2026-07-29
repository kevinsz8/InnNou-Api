-- =============================================================
-- MIGRATION: Add DeliveryNoteNumber to GoodsReceipt
-- Date: 2026-07-29
-- =============================================================
-- The Asociado's own warehouse staff often already track deliveries against
-- a supplier's delivery note ("albarán") number from their own paper/ERP
-- process, independent of InnNou's PurchaseOrderNumber. A PurchaseOrder can
-- be received across multiple separate GoodsReceipts (partial, then the
-- rest later) — each one is its own physical delivery with its own albarán
-- number, so this belongs on GoodsReceipt (one per physical delivery), not
-- on PurchaseOrder (one per order, potentially several deliveries).
--
-- Required going forward (validated in CreateGoodsReceiptCommandHandler),
-- but added nullable + backfilled first since existing rows predate the
-- field and are immutable historical records (GoodsReceipt is append-only,
-- never edited after creation) — 'N/A' is a clearly-flagged placeholder,
-- not a fabricated value. Guarded so it is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('GoodsReceipt') AND name = 'DeliveryNoteNumber')
BEGIN
    ALTER TABLE GoodsReceipt ADD DeliveryNoteNumber nvarchar(100) NULL;
END
GO

UPDATE GoodsReceipt SET DeliveryNoteNumber = 'N/A' WHERE DeliveryNoteNumber IS NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('GoodsReceipt') AND name = 'DeliveryNoteNumber' AND is_nullable = 1)
BEGIN
    ALTER TABLE GoodsReceipt ALTER COLUMN DeliveryNoteNumber nvarchar(100) NOT NULL;
END
GO
