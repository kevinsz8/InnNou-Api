-- =============================================================
-- MIGRATION: Add "Conciliar Factura" sidebar entry under groupFinance
-- Date: 2026-08-01
-- =============================================================
-- Sibling of the existing "invoices" menu item (relabeled to "Consultar
-- Facturas" in i18n only, no DB change needed there — MenuItems.Name is
-- just the i18n key, the row itself is untouched). This new row points at
-- the create/match page (/supplierInvoices/create) and mirrors
-- SupplierInvoiceService.CanManageSupplierInvoicesAsync's own gate exactly:
-- ASSOCIATE organizations only, RoleLevel >= 80 (Admin+) only — no bare-
-- SuperAdmin bypass, same as the underlying create endpoint itself. Unlike
-- "invoices" (ungated by OrganizationTypeId, since reading has no RoleLevel
-- floor), this row combines both the Role and OrganizationType dimensions,
-- same pattern as 20260726_MenuItems_AddConsolidatedPurchaseOrdersReport.sql.
--
-- The menu system has no native "RoleLevel >= X" runtime operator
-- (sp_MenuItem_GetVisibleForContext does an exact RoleId match resolved
-- from the caller's RoleLevel) — so the >= 80 threshold is simulated by
-- enumerating every currently-qualifying RoleId at migration-insert time.
-- If a future role is ever added at RoleLevel >= 80, it needs a matching
-- MenuAssignments row inserted the same way.
--
-- Idempotent — safe to re-run.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'invoiceReconciliation', '/supplierInvoices/create', 'invoices', 3, 'System'
FROM MenuItems p
WHERE p.Name = 'groupFinance' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'invoiceReconciliation' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, OrganizationTypeId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, ot.OrganizationTypeId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
CROSS JOIN OrganizationTypes ot
WHERE m.Name = 'invoiceReconciliation'
  AND m.ParentMenuItemId = (SELECT MenuItemId FROM MenuItems WHERE Name = 'groupFinance' AND ParentMenuItemId IS NULL)
  AND r.RoleLevel >= 80
  AND ot.Code = 'ASSOCIATE'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId
        AND existing.RoleId = r.RoleId
        AND existing.OrganizationTypeId = ot.OrganizationTypeId
  );
GO

PRINT '=== Migration 20260801_MenuItems_AddSupplierInvoiceReconciliation completed successfully ===';
GO
