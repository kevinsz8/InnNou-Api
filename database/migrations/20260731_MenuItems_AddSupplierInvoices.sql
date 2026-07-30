-- =============================================================
-- MIGRATION: Add "Facturas" (Supplier Invoices) sidebar entry under groupFinance
-- Date: 2026-07-31
-- =============================================================
-- Facturación Phase B (see .claude/GoodsReceiptsModule.md's Facturacion section
-- and TODO.md point 5) — the frontend already carried a dead "invoices"
-- i18n label ("Facturas") and icon-map entry as a stale placeholder from
-- before this module existed (flagged in CLAUDE.md history); this migration
-- is what makes it live by inserting the matching MenuItems row with
-- Name = 'invoices' pointed at the real /supplierInvoices route, so no
-- frontend menu-key change was needed.
--
-- Same non-Supplier-role, no-OrganizationTypeId-restriction assignment set
-- as FamilyApprovalThresholds (groupFinance's other child) — reading the
-- list has no RoleLevel floor server-side (SupplierInvoiceService's
-- CanReadOrganizationAsync mirrors PurchaseOrderService's own read gate);
-- only creating an invoice is Admin+ (RoleLevel >= 80), gated client-side
-- and enforced server-side regardless of what the sidebar shows.
--
-- Idempotent — safe to re-run.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'invoices', '/supplierInvoices', 'invoices', 2, 'System'
FROM MenuItems p
WHERE p.Name = 'groupFinance' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'invoices' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, OrganizationTypeId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, NULL, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
WHERE m.Name = 'invoices'
  AND m.ParentMenuItemId = (SELECT MenuItemId FROM MenuItems WHERE Name = 'groupFinance' AND ParentMenuItemId IS NULL)
  AND r.NormalizedName <> 'SUPPLIER'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId
        AND existing.RoleId = r.RoleId
        AND existing.OrganizationTypeId IS NULL
  );
GO

PRINT '=== Migration 20260731_MenuItems_AddSupplierInvoices completed successfully ===';
GO
