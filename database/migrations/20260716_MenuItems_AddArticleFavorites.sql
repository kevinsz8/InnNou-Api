-- =============================================================
-- MIGRATION: Add "Article Favorites" sidebar menu item
-- Date: 2026-07-16
-- =============================================================
-- Adds the Article Favorites page (ArticleFavorites feature, see
-- 20260716_ArticleFavorites_Create.sql) to the Procurement group,
-- alongside Suppliers(1)/Articles(2)/Warehouses(3). New "favorites"
-- icon key added to the frontend's DashboardLayout.tsx ICONS map in
-- the same change.
--
-- Restricted from the Supplier role via MenuAssignments, same
-- reasoning as 20260715_MenuItems_AddWarehouses.sql: a Supplier-scoped
-- session has no OrganizationId, so ArticleFavoriteService throws
-- ARTICLE_FAVORITE_NO_ORGANIZATION_CONTEXT for every read/write a
-- Supplier session would attempt — showing the nav item to suppliers
-- would only lead to an empty, non-functional page.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'articleFavorites', '/article-favorites', 'favorites', 4, 'System'
FROM MenuItems p
WHERE p.Name = 'groupProcurement' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'articleFavorites' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
WHERE m.Name = 'articleFavorites'
  AND r.NormalizedName <> 'SUPPLIER'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId AND existing.RoleId = r.RoleId
  );
GO
