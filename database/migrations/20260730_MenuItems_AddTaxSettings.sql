-- =============================================================
-- MIGRATION: Add "Impuestos" (Tax settings) sidebar entry under groupAdmin
-- Date: 2026-07-30
-- =============================================================
-- New SuperAdmin-only page to view/edit TaxJurisdictions x TaxCategories
-- rate grid (Tax module Phase A — see .claude/GoodsReceiptsModule.md's tax
-- section). A jurisdiction's tax rate is a legal fact, not an org-scoped
-- business setting, so this is RoleId=SUPERADMIN only, no OrganizationTypeId
-- dimension needed (NULL = wildcard, same shape as 'users'/'organizations').
--
-- Idempotent — safe to re-run.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'taxSettings', '/impuestos', 'taxSettings', 3, 'System'
FROM MenuItems p
WHERE p.Name = 'groupAdmin' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'taxSettings' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, OrganizationTypeId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, NULL, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
WHERE m.Name = 'taxSettings'
  AND r.NormalizedName = 'SUPERADMIN'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId
        AND existing.RoleId = r.RoleId
        AND existing.OrganizationTypeId IS NULL
  );
GO

PRINT '=== Migration 20260730_MenuItems_AddTaxSettings completed successfully ===';
GO
