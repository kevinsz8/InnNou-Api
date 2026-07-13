-- =============================================================
-- MIGRATION: Restrict the Supplier role's sidebar menu
-- Date: 2026-07-13
-- =============================================================
-- SuperAdmin/Admin (and Manager/Supervisor/Employee, unaffected — not asked to
-- restrict them) keep seeing everything, since the items below stay ungoverned
-- for every role except Supplier. A Supplier session — whether a real supplier
-- login or an Admin impersonating a supplier — ends up with RoleLevel = 10
-- either way: /auth/impersonate-supplier reissues the JWT with the *impersonated*
-- user's own RoleLevel (see AuthService.ImpersonateAsync), not the actor's. So
-- restricting by the Supplier RoleId alone covers both cases uniformly.
--
-- Per sp_MenuItem_GetVisibleForContext's resolution rule, once an item has any
-- MenuAssignments row it's hidden from any role with no matching row. This
-- inserts an ALLOW row for every non-Supplier role on each item that should be
-- hidden from suppliers, leaving Home/Catalogs-group/Families/Procurement-group/
-- Articles untouched (ungoverned = still visible to everyone, including
-- suppliers) — suppliers end up seeing only Home, Families (under Catalogs) and
-- Articles (which is where ArticlePrices management already lives — there's no
-- standalone "Prices" route/menu item, see CLAUDE.md debt item #14).
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

INSERT INTO MenuAssignments (MenuItemId, RoleId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
WHERE m.Name IN ('categories', 'unitTypes', 'unitsOfMeasure', 'suppliers', 'groupAdmin', 'organizations', 'users')
  AND r.NormalizedName <> 'SUPPLIER'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId AND existing.RoleId = r.RoleId
  );
GO
