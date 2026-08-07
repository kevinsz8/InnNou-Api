-- =============================================================
-- MIGRATION: Add "Notas de Crédito" (Supplier Credit Notes) sidebar entry under groupFinance
-- Date: 2026-08-07
-- =============================================================
-- Facturación Phase C (see .claude/SupplierCreditNoteModule.md and TODO.md point 5) —
-- same non-Supplier-role, no-OrganizationTypeId-restriction assignment set as
-- FamilyApprovalThresholds/Facturas/Conciliar Factura (groupFinance's other children).
-- Reading the list has no RoleLevel floor server-side; only creating a credit note is
-- Admin+ (RoleLevel >= 80), gated client-side (contextual button on SupplierReturnDetail)
-- and enforced server-side regardless of what the sidebar shows.
--
-- Idempotent — safe to re-run.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'supplierCreditNotes', '/supplierCreditNotes', 'supplierCreditNotes', 4, 'System'
FROM MenuItems p
WHERE p.Name = 'groupFinance' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'supplierCreditNotes' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, OrganizationTypeId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, NULL, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
WHERE m.Name = 'supplierCreditNotes'
  AND m.ParentMenuItemId = (SELECT MenuItemId FROM MenuItems WHERE Name = 'groupFinance' AND ParentMenuItemId IS NULL)
  AND r.NormalizedName <> 'SUPPLIER'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId
        AND existing.RoleId = r.RoleId
        AND existing.OrganizationTypeId IS NULL
  );
GO

PRINT '=== Migration 20260807_MenuItems_AddSupplierCreditNotes completed successfully ===';
GO
