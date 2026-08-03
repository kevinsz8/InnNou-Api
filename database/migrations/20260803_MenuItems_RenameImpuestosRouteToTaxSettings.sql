-- =============================================================
-- MIGRATION: Rename the "Impuestos" menu item's Route to /taxSettings
-- Date: 2026-08-03
-- =============================================================
-- 20260730_MenuItems_AddTaxSettings.sql already used the correct English
-- Name ('taxSettings') but left the Route itself in Spanish (/impuestos) —
-- inconsistent with every other route in the app (/goodsReceipts,
-- /supplierReturns, /orders, ...). This just fixes the Route column; the
-- Spanish label the user actually sees still comes from menu.taxSettings in
-- the i18n files, unchanged.
--
-- Idempotent — safe to re-run.
-- =============================================================

UPDATE MenuItems
SET Route = '/taxSettings'
WHERE Name = 'taxSettings' AND ParentMenuItemId IS NOT NULL AND Route = '/impuestos';
GO

PRINT '=== Migration 20260803_MenuItems_RenameImpuestosRouteToTaxSettings completed successfully ===';
GO
