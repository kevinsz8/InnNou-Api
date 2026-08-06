/* =============================================================
   UNIT-AWARE QUANTITIES — internal stock-handling entry points
   Researched against SAP (dedicated "Unit of Issue" field, separate from
   Order Unit), Oracle Hospitality Materials Control (requisition-line unit
   vs. item's Unit of Issue, with a rounding factor), and Odoo (Purchase UoM
   vs. Inventory UoM, internal transfers use the latter) before building —
   all three separate "the unit you buy in" from "the unit you move/count
   internally", specifically so staff never have to enter a fractional
   purchase-unit quantity (e.g. "0.25 Caja") when a smaller packaging level
   (e.g. "6 Botella") is the natural ask.

   Scope, confirmed with the user: Requisitions + Inventory (Adjustments,
   Transfers, Period Counts) — anywhere staff handle/count physical stock
   internally. Orders/PurchaseOrders/GoodsReceipts are deliberately OUT of
   scope and stay denominated in Article.PurchaseUnitId only — that's the
   actual contract unit with the supplier, matching every system above.

   Storage strategy, confirmed with the user: normalize + keep the original.
   The existing "canonical" quantity column on each table (QuantityRequested,
   QuantityIssued, Quantity, CountedQuantity) keeps meaning exactly what it
   already means everywhere else in the app — Article.PurchaseUnitId-
   denominated, zero changes needed in StockLevels/ParLevels/reporting/any
   other consumer. The new nullable pair on each table records what the user
   actually typed (unit + quantity) purely for accurate re-display and audit
   — NULL means "entered directly in the Purchase Unit," preserving exact
   backward compatibility for any caller that never sends a unit.

   No new conversion-rate table is needed: Article.PurchaseUnitId together
   with that article's own ArticlePackagingLevels chain (QuantityInParentUnit
   at each level) already encodes every conversion factor required — see
   InnNou.Application.Common.ArticleUnitConversion (new) for the shared
   helper, which every one of these write paths now calls.

   RECEIPT movements (Goods Receipts, out of scope) are the only
   InventoryMovements type that will always have EnteredUnitId/EnteredQuantity
   NULL; a CONSUMPTION movement copies its RequisitionIssueLine's
   IssuedUnitId/IssuedQuantity forward for full audit-trail fidelity.
   ============================================================= */

ALTER TABLE dbo.RequisitionLines ADD
    RequestedUnitId   INT           NULL CONSTRAINT FK_RequisitionLines_RequestedUnit REFERENCES dbo.UnitsOfMeasure(UnitOfMeasureId),
    RequestedQuantity DECIMAL(18,8) NULL;
GO

ALTER TABLE dbo.RequisitionIssueLines ADD
    IssuedUnitId   INT           NULL CONSTRAINT FK_RequisitionIssueLines_IssuedUnit REFERENCES dbo.UnitsOfMeasure(UnitOfMeasureId),
    IssuedQuantity DECIMAL(18,8) NULL;
GO

ALTER TABLE dbo.InventoryMovements ADD
    EnteredUnitId   INT           NULL CONSTRAINT FK_InventoryMovements_EnteredUnit REFERENCES dbo.UnitsOfMeasure(UnitOfMeasureId),
    EnteredQuantity DECIMAL(18,8) NULL;
GO

ALTER TABLE dbo.InventoryPeriodCounts ADD
    CountedUnitId         INT           NULL CONSTRAINT FK_InventoryPeriodCounts_CountedUnit REFERENCES dbo.UnitsOfMeasure(UnitOfMeasureId),
    CountedQuantityInUnit DECIMAL(18,8) NULL;
GO
