-- =============================================================
-- MIGRATION: Add GoodsReceipt.DeliveryNoteDate
-- Date: 2026-08-03
-- =============================================================
-- GoodsReceipt already captures the supplier's own delivery note NUMBER
-- (DeliveryNoteNumber, added 2026-07-29) but not a delivery note DATE — the
-- date printed on the supplier's own albaran, which can differ from
-- CreatedUtc (when the buyer's staff got around to recording the receipt in
-- InnNou). Nullable and optional: existing rows and any future receipt
-- created without typing a date stay NULL, same "don't fabricate a value the
-- system doesn't actually know" rule as every other optional field here.
--
-- Idempotent — safe to re-run.
-- =============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.GoodsReceipt') AND name = 'DeliveryNoteDate'
)
BEGIN
    ALTER TABLE dbo.GoodsReceipt ADD DeliveryNoteDate DATE NULL;
END
GO

PRINT '=== Migration 20260803_GoodsReceipt_AddDeliveryNoteDate completed successfully ===';
GO
