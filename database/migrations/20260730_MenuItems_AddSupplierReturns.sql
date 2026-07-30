-- =============================================================
-- MIGRATION: Add "Supplier Returns" sidebar menu item
-- Date: 2026-07-30
-- =============================================================
-- Adds the new SupplierReturns page (devolucion a proveedor / RMA — TODO.md
-- point 2) to the Operaciones group, alongside Warehouses(1)/Orders(2)/
-- OrderTemplates(3)/ArticleFavorites(4)/PendingApprovals(5)/Inventory(6).
-- Restricted from the Supplier role, same reasoning as every other
-- Operaciones addition — this feature is 100% buyer-side. New
-- "supplierReturns" icon key added to the frontend's DashboardLayout.tsx
-- ICONS map in the same change.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'supplierReturns', '/supplierReturns', 'supplierReturns', 7, 'System'
FROM MenuItems p
WHERE p.Name = 'groupOperations' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'supplierReturns' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
WHERE m.Name = 'supplierReturns'
  AND r.NormalizedName <> 'SUPPLIER'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId AND existing.RoleId = r.RoleId
  );
GO
