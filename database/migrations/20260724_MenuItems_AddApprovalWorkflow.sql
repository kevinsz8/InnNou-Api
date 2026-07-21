-- =============================================================
-- MIGRATION: Add "Approval Thresholds" and "Pending Approvals" sidebar menu items
-- Date: 2026-07-24
-- =============================================================
-- Adds the Order Approval Workflow pages (see 20260724_FamilyApprovalThresholds_Create.sql /
-- OrderApprovalSteps_Create.sql) to the Procurement group, alongside Suppliers(1)/Articles(2)/
-- Warehouses(3)/ArticleFavorites(4)/Orders(5)/OrderTemplates(6)/DeliveryZones(7)/
-- ArticleClassification(8). New "approvalThresholds"/"pendingApprovals" icon keys added to the
-- frontend's DashboardLayout.tsx ICONS map in the same change.
--
-- Restricted from the Supplier role via MenuAssignments, same reasoning as every other
-- Procurement addition this cycle: a Supplier-scoped session has no OrganizationId, so these
-- pages (always keyed off the caller's own organization) would show/do nothing useful for that
-- role.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'familyApprovalThresholds', '/family-approval-thresholds', 'approvalThresholds', 9, 'System'
FROM MenuItems p
WHERE p.Name = 'groupProcurement' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'familyApprovalThresholds' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'pendingApprovals', '/pending-approvals', 'pendingApprovals', 10, 'System'
FROM MenuItems p
WHERE p.Name = 'groupProcurement' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'pendingApprovals' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
WHERE m.Name IN ('familyApprovalThresholds', 'pendingApprovals')
  AND r.NormalizedName <> 'SUPPLIER'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId AND existing.RoleId = r.RoleId
  );
GO
