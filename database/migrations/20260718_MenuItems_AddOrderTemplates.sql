-- =============================================================
-- MIGRATION: Add "Order Templates" sidebar menu item
-- Date: 2026-07-18
-- =============================================================
-- Adds the Order Templates management page to the Procurement group,
-- alongside Suppliers(1)/Articles(2)/Warehouses(3)/ArticleFavorites(4)/
-- Orders(5). New "orderTemplates" icon key added to the frontend's
-- DashboardLayout.tsx ICONS map in the same change.
--
-- Restricted from the Supplier role via MenuAssignments, same reasoning
-- as the Orders/Warehouses/ArticleFavorites migrations before it: a
-- Supplier-scoped session has no OrganizationId, so OrderTemplateService's
-- CanAccessTemplateAsync resolves to "no access" for it anyway — showing
-- the nav item would only lead to an empty, non-functional page.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'orderTemplates', '/order-templates', 'orderTemplates', 6, 'System'
FROM MenuItems p
WHERE p.Name = 'groupProcurement' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'orderTemplates' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
WHERE m.Name = 'orderTemplates'
  AND r.NormalizedName <> 'SUPPLIER'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId AND existing.RoleId = r.RoleId
  );
GO
