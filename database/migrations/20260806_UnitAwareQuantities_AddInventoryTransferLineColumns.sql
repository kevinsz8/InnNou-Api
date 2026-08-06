/* =============================================================
   UNIT-AWARE QUANTITIES — addendum: InventoryTransferLines
   Missed in the original 20260806_UnitAwareQuantities_AddColumns.sql pass —
   InventoryTransferLines.Quantity is the canonical, PurchaseUnitId-
   denominated value shown on the Transfer's own detail page (not just the
   InventoryMovements audit ledger, which already got its own pair). Same
   "normalize + keep the original" pattern as every other table in this
   feature — see the original migration's header comment for the full design.
   ============================================================= */

ALTER TABLE dbo.InventoryTransferLines ADD
    TransferredUnitId   INT           NULL CONSTRAINT FK_InventoryTransferLines_TransferredUnit REFERENCES dbo.UnitsOfMeasure(UnitOfMeasureId),
    TransferredQuantity DECIMAL(18,8) NULL;
GO
