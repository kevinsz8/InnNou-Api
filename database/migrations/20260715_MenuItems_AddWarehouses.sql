-- =============================================================
-- MIGRATION: Add "Warehouses" sidebar menu item
-- Date: 2026-07-15
-- =============================================================
-- Adds the Warehouses page (see 20260715_Warehouses_Create.sql) to the
-- Procurement group, alongside Suppliers/Articles. Reuses the existing
-- "inventory" icon key — already mapped in the frontend's DashboardLayout.tsx
-- ICONS object but unused until now (InnNou-Web CLAUDE.md debt item #14).
--
-- Restricted from the Supplier role via MenuAssignments, same reasoning as
-- 20260713_MenuAssignments_RestrictSupplierMenu.sql: a Supplier-scoped
-- session has no OrganizationId, so every Warehouse read/write resolves to
-- "no organization" server-side anyway (WarehouseService.CanManageReadAsync/
-- CanManageOrganizationAsync both require context.OrganizationId) — showing
-- the nav item to suppliers would only lead to an empty, non-functional page.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'warehouses', '/warehouses', 'inventory', 3, 'System'
FROM MenuItems p
WHERE p.Name = 'groupProcurement' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'warehouses' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
WHERE m.Name = 'warehouses'
  AND r.NormalizedName <> 'SUPPLIER'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId AND existing.RoleId = r.RoleId
  );
GO
