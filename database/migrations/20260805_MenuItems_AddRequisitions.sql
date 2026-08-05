-- =============================================================
-- MIGRATION: Add "Departamentos" and "Requisiciones Internas" sidebar menu items
-- Date: 2026-08-05
-- =============================================================
-- Requisiciones Internas ("Store Requisition") is the "stock going out" process the
-- Inventory/GoodsReceipts/PurchaseOrder chain never covered — a Department (per-Organization,
-- hand-managed on its own admin page) requests Articles from its own Organization's Warehouse;
-- the Warehouse then issues stock against it. Adds two new standalone pages to the Operaciones
-- group, alongside Warehouses(1)/Orders(2)/OrderTemplates(3)/ArticleFavorites(4)/
-- PendingApprovals(5)/Inventory(6)/SupplierReturns(7)/GoodsReceipts(8)/InternalOrders(9).
--
-- Restricted to OrganizationTypeId = ASSOCIATE only (same filter mechanism as GoodsReceipts'
-- and InternalOrders' own migrations) — a Requisition is always acted on at the property level;
-- a SUPER_ASSOCIATE session has no warehouse/department of its own to manage.
--
-- Excludes the Supplier role for the same reason as every other Operaciones item: a
-- Supplier-scoped session has no OrganizationId, so these pages would always resolve to
-- "no access" anyway. New "departments"/"requisitions" icon keys added to the frontend's
-- DashboardLayout.tsx ICONS map in the same change.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'departments', '/departments', 'departments', 10, 'System'
FROM MenuItems p
WHERE p.Name = 'groupOperations' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'departments' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'requisitions', '/requisitions', 'requisitions', 11, 'System'
FROM MenuItems p
WHERE p.Name = 'groupOperations' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'requisitions' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, OrganizationTypeId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, ot.OrganizationTypeId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
CROSS JOIN OrganizationTypes ot
WHERE m.Name IN ('departments', 'requisitions')
  AND r.NormalizedName <> 'SUPPLIER'
  AND ot.Code = 'ASSOCIATE'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId
        AND existing.RoleId = r.RoleId
        AND existing.OrganizationTypeId = ot.OrganizationTypeId
  );
GO

PRINT '=== Migration 20260805_MenuItems_AddRequisitions completed successfully ===';
GO
