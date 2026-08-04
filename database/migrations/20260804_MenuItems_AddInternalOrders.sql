-- =============================================================
-- MIGRATION: Add "Pedidos Internos" (Internal Orders) sidebar menu item
-- Date: 2026-08-04
-- =============================================================
-- Internal Orders is a deliberately separate domain from Traspasos (Inventory
-- Transfers, same-Organization only) and from Orders/PurchaseOrder — it moves
-- stock BETWEEN two different Asociado Organizations under the same Super
-- Asociado. Adds the new standalone /internalOrders page to the Operaciones
-- group, alongside Warehouses(1)/Orders(2)/OrderTemplates(3)/
-- ArticleFavorites(4)/PendingApprovals(5)/Inventory(6)/SupplierReturns(7)/
-- GoodsReceipts(8).
--
-- Restricted to OrganizationTypeId = ASSOCIATE only (same filter mechanism as
-- GoodsReceipts' own 20260803_MenuItems_AddGoodsReceipts.sql) — an Internal
-- Order is always acted on at the property level as either the requesting or
-- the source side, mirroring InternalOrderService.CanManageOrganizationAsync's
-- own ASSOCIATE-only write gate. A SUPER_ASSOCIATE session has no legitimate
-- use for this page.
--
-- Excludes the Supplier role for the same reason as every other Operaciones
-- item: a Supplier-scoped session has no OrganizationId, so this page would
-- always resolve to "no access" anyway. New "internalOrders" icon key added
-- to the frontend's DashboardLayout.tsx ICONS map in the same change.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'internalOrders', '/internalOrders', 'internalOrders', 9, 'System'
FROM MenuItems p
WHERE p.Name = 'groupOperations' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'internalOrders' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, OrganizationTypeId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, ot.OrganizationTypeId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
CROSS JOIN OrganizationTypes ot
WHERE m.Name = 'internalOrders'
  AND r.NormalizedName <> 'SUPPLIER'
  AND ot.Code = 'ASSOCIATE'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId
        AND existing.RoleId = r.RoleId
        AND existing.OrganizationTypeId = ot.OrganizationTypeId
  );
GO

PRINT '=== Migration 20260804_MenuItems_AddInternalOrders completed successfully ===';
GO
