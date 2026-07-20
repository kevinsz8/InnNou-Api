-- =============================================================
-- MIGRATION: Add "Zones" and "Delivery Zones" sidebar menu items
-- Date: 2026-07-21
-- =============================================================
-- "zones" — Catalog admin page (Country/Zone CRUD), added to the Catalogs
-- group alongside Categories(1)/Families(2)/UnitTypes(3)/UnitsOfMeasure(4),
-- same "no MenuAssignments floor beyond role-based visibility, gated inside
-- the page/API itself" pattern as its siblings. Restricted from the
-- Supplier role — a Supplier-scoped session has no use for the zone
-- catalog admin page (it only ever picks an existing zone via search).
--
-- "deliveryZones" — the standalone "ZonasEntrega" page where a Supplier
-- (or an Admin/SuperAdmin impersonating/picking one) declares its own
-- per-zone delivery days. Added to the Procurement group right after
-- Orders(5)/OrderTemplates(6). Deliberately DOES include the Supplier
-- role in MenuAssignments — unlike Orders/OrderTemplates/Warehouses/
-- ArticleFavorites (which exclude Supplier because a Supplier-scoped
-- session has no OrganizationId for those services to resolve), a
-- Supplier's own login IS the primary intended user of this page.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'zones', '/catalog/zones', 'zones', 5, 'System'
FROM MenuItems p
WHERE p.Name = 'groupCatalogs' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'zones' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

INSERT INTO MenuAssignments (MenuItemId, RoleId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
WHERE m.Name = 'zones'
  AND r.NormalizedName <> 'SUPPLIER'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId AND existing.RoleId = r.RoleId
  );
GO

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT p.MenuItemId, 'deliveryZones', '/supplier-delivery-zones', 'deliveryZones', 7, 'System'
FROM MenuItems p
WHERE p.Name = 'groupProcurement' AND p.ParentMenuItemId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM MenuItems existing
      WHERE existing.Name = 'deliveryZones' AND existing.ParentMenuItemId = p.MenuItemId
  );
GO

-- Every role, INCLUDING Supplier — see header comment.
INSERT INTO MenuAssignments (MenuItemId, RoleId, IsAllowed, CreatedBy)
SELECT m.MenuItemId, r.RoleId, 1, 'System'
FROM MenuItems m
CROSS JOIN Roles r
WHERE m.Name = 'deliveryZones'
  AND NOT EXISTS (
      SELECT 1 FROM MenuAssignments existing
      WHERE existing.MenuItemId = m.MenuItemId AND existing.RoleId = r.RoleId
  );
GO
