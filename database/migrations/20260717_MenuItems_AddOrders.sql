-- =============================================================
-- MIGRATION: Add "Orders" sidebar menu item
-- Date: 2026-07-17
-- =============================================================
-- Adds the Orders module (Order/OrderCreate/OrderDetail pages, see the
-- Orders module in InnNou-Api CLAUDE.md) to the Procurement group,
-- alongside Suppliers(1)/Articles(2)/Warehouses(3)/ArticleFavorites(4).
-- New "orders" icon key added to the frontend's DashboardLayout.tsx
-- ICONS map in the same change.
--
-- Restricted from the Supplier role via MenuAssignments, same reasoning
-- as 20260715_MenuItems_AddWarehouses.sql/20260716_MenuItems_AddArticleFavorites.sql:
-- a Supplier-scoped session has no OrganizationId, so OrderService's
-- CanManageOrganizationAsync/GetPagedAsync both resolve to "no access"
-- for a Supplier login anyway — showing the nav item would only lead to
-- an empty, non-functional page.
--
-- IMPORTANT: unlike the Supplier exclusion above, a SUPER_ASSOCIATE-type
-- organization's users ARE included in the MenuAssignments below — they
-- get the nav item and full read access (list + detail). The
-- SUPER_ASSOCIATE-vs-ASSOCIATE write restriction (create/edit/submit/
-- cancel/delete) is enforced separately inside OrderService.
-- CanManageOrganizationAsync and the frontend's page-level gating, not
-- by hiding this menu item.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'orders', '/orders', 'orders', 5, 'System'
FROM MenuItems p
WHERE p.Name = 'groupProcurement' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'orders' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
WHERE m.Name = 'orders'
  AND r.NormalizedName <> 'SUPPLIER'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId AND existing.RoleId = r.RoleId
  );
GO
