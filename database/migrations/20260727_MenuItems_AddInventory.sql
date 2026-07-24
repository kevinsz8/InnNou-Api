-- =============================================================
-- MIGRATION: Add "Inventory" sidebar menu item
-- Date: 2026-07-27
-- =============================================================
-- Adds the new Inventory page (stock levels, adjustments, transfers) to the
-- Operaciones group, alongside Warehouses(1)/Orders(2)/OrderTemplates(3)/
-- ArticleFavorites(4)/PendingApprovals(5). Restricted from the Supplier role,
-- same reasoning as every other Operaciones addition — a Supplier-scoped
-- session has no OrganizationId, so a Warehouse-keyed page would show/do
-- nothing useful for that role. New "stockLevels" icon key added to the
-- frontend's DashboardLayout.tsx ICONS map in the same change (the plain
-- "inventory" key was already taken — it's Warehouses' own menu icon).
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'inventory', '/inventory', 'stockLevels', 6, 'System'
FROM MenuItems p
WHERE p.Name = 'groupOperations' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'inventory' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
WHERE m.Name = 'inventory'
  AND r.NormalizedName <> 'SUPPLIER'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId AND existing.RoleId = r.RoleId
  );
GO
