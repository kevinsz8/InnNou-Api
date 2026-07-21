-- =============================================================
-- MIGRATION: Add "Article Classification" sidebar menu item
-- Date: 2026-07-23
-- =============================================================
-- Adds the Article Classification page (ArticleClassifications feature, see
-- 20260723_ArticleClassifications_Create.sql) to the Procurement group,
-- alongside Suppliers(1)/Articles(2)/Warehouses(3)/ArticleFavorites(4)/
-- Orders(5)/OrderTemplates(6)/DeliveryZones(7). New "classification" icon key
-- added to the frontend's DashboardLayout.tsx ICONS map in the same change.
--
-- Restricted from the Supplier role via MenuAssignments, same reasoning as
-- 20260716_MenuItems_AddArticleFavorites.sql: a Supplier-scoped session has
-- no OrganizationId, so classification (which is always keyed off the
-- caller's own organization) would show/do nothing useful for that role.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'articleClassification', '/article-classification', 'classification', 8, 'System'
FROM MenuItems p
WHERE p.Name = 'groupProcurement' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'articleClassification' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
WHERE m.Name = 'articleClassification'
  AND r.NormalizedName <> 'SUPPLIER'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId AND existing.RoleId = r.RoleId
  );
GO
