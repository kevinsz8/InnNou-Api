-- =============================================================
-- MIGRATION: Add "Recepciones" (Goods Receipts) sidebar menu item
-- Date: 2026-08-03
-- =============================================================
-- Goods Receipts shipped 2026-07-26 with no sidebar entry — direct-URL-only
-- (/purchaseOrders/:token/receive, reached solely by clicking "Recibir" on a
-- PurchaseOrder card in OrderDetail.tsx), a deliberate gap flagged in
-- .claude/GoodsReceiptsModule.md at the time ("no standalone page or sidebar
-- entry"). Adds the new standalone /goodsReceipts history/search page to
-- the Operaciones group, alongside Warehouses(1)/Orders(2)/OrderTemplates(3)/
-- ArticleFavorites(4)/PendingApprovals(5)/Inventory(6)/SupplierReturns(7).
--
-- UNLIKE Orders (20260717_MenuItems_AddOrders.sql), which explicitly keeps a
-- SUPER_ASSOCIATE-type organization's users in its MenuAssignments for
-- read-only visibility, this item is ALSO restricted by OrganizationTypeId =
-- ASSOCIATE (same filter mechanism 20260726_MenuAssignments_AddOrganizationTypeFilter.sql
-- introduced for Consolidated Purchase Orders, just the opposite org type) —
-- confirmed with the user. This mirrors PurchaseOrderService.CreateGoodsReceiptAsync's
-- own authorization, which deliberately does NOT extend Orders' SUPER_ASSOCIATE
-- read-only carve-out: "Same OrganizationTypeCode == ASSOCIATE-only rule as
-- Cancel/Rectify (... an impersonated SUPER_ASSOCIATE caller cannot create a
-- receipt)" — a SUPER_ASSOCIATE session has zero use for this page, unlike
-- Orders where it's a legitimate read-only oversight view.
--
-- Excludes the Supplier role for the same reason as every other Operaciones
-- item: a Supplier-scoped session has no OrganizationId, so this page would
-- always resolve to "no access" anyway. New "goodsReceipts" icon key added
-- to the frontend's DashboardLayout.tsx ICONS map in the same change.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'goodsReceipts', '/goodsReceipts', 'goodsReceipts', 8, 'System'
FROM MenuItems p
WHERE p.Name = 'groupOperations' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'goodsReceipts' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, OrganizationTypeId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, ot.OrganizationTypeId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
CROSS JOIN OrganizationTypes ot
WHERE m.Name = 'goodsReceipts'
  AND r.NormalizedName <> 'SUPPLIER'
  AND ot.Code = 'ASSOCIATE'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId
        AND existing.RoleId = r.RoleId
        AND existing.OrganizationTypeId = ot.OrganizationTypeId
  );
GO

PRINT '=== Migration 20260803_MenuItems_AddGoodsReceipts completed successfully ===';
GO
