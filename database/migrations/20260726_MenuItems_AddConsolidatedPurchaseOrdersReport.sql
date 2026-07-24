-- =============================================================
-- MIGRATION: Add "Reports" sidebar group with Consolidated Purchase Orders
-- Date: 2026-07-26
-- =============================================================
-- Consolidated Purchase Orders (see .claude/ConsolidatedPurchaseOrderModule.md)
-- shipped 2026-07-23 with no sidebar menu entry — direct-URL-only, a
-- deliberate, flagged gap (the menu system had no OrganizationTypeCode
-- filter dimension yet, see 20260726_MenuAssignments_AddOrganizationTypeFilter.sql).
--
-- New top-level "Reports" group (SortOrder 7, after groupAdmin=6) with a
-- single child for now. Both the group header and the child are restricted
-- to non-Supplier roles AND OrganizationTypeId = SUPER_ASSOCIATE — mirrors
-- exactly what useCanAccessConsolidatedPurchaseOrders() already checks
-- frontend-side (isStaffOrAbove && isSuperAssociateOrg), so a bare SuperAdmin
-- (no OrganizationTypeCode claim until impersonating a Super Asociado org)
-- won't see it either, consistent with the page's own access rule.
--
-- Restricting only the child and not the parent would leave an empty
-- "Reports" group header visible to everyone (sp_MenuItem_GetVisibleForContext
-- returns a group header with zero MenuAssignments rows to everyone
-- regardless of its children's visibility) — so both rows get the same
-- assignment set, per the existing comment in
-- sp_MenuItem_GetVisibleForContext ("restricting a parent does not
-- automatically restrict its children").
--
-- Idempotent — safe to re-run.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT NULL, 'groupReports', NULL, NULL, 7, 'System'
WHERE NOT EXISTS (SELECT 1 FROM MenuItems WHERE Name = 'groupReports' AND ParentMenuItemId IS NULL);
GO

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'consolidatedPurchaseOrders', '/consolidated-purchase-orders', 'consolidatedPurchaseOrders', 1, 'System'
FROM MenuItems p
WHERE p.Name = 'groupReports' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'consolidatedPurchaseOrders' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, OrganizationTypeId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, ot.OrganizationTypeId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
CROSS JOIN OrganizationTypes ot
WHERE m.Name IN ('groupReports', 'consolidatedPurchaseOrders')
  AND r.NormalizedName <> 'SUPPLIER'
  AND ot.Code = 'SUPER_ASSOCIATE'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId
        AND existing.RoleId = r.RoleId
        AND existing.OrganizationTypeId = ot.OrganizationTypeId
  );
GO

PRINT '=== Migration 20260726_MenuItems_AddConsolidatedPurchaseOrdersReport completed successfully ===';
GO
