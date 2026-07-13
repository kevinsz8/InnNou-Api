-- =============================================================
-- MIGRATION: Redesign MenuItems / MenuAssignments for the dynamic sidebar menu
-- Date: 2026-07-13
-- =============================================================
-- The v1 MenuItems/MenuAssignments tables (created 2026-06-27 as part of the
-- initial DB scaffold) are orphaned: zero C# references, zero stored
-- procedures, and stale seed data referencing pre-rename "/hotels" routes
-- and placeholder role tags ("SUPER_ASSOCIATE"/"ASSOCIATE") that were never
-- real Roles rows. Confirmed via full-repo grep + git blame before dropping,
-- and the drop was explicitly confirmed by the user before this file was
-- written (dev DB only, no application code references either table).
--
-- This replaces them with the shape documented in .claude/database-v2.md
-- ("Menu visibility is database driven. Do NOT hardcode menu visibility in
-- React. Visibility may depend on: Role, Organization, Supplier."), with one
-- naming deviation from that doc: MenuItemToken instead of MenuToken, to
-- match the {Entity}Token convention used by every other table.
--
-- Resolution rule (enforced in sp_MenuItem_GetVisibleForContext, not here):
-- a MenuItem with zero MenuAssignments rows is visible to everyone. Once it
-- has at least one row, it's visible only if a row matches the caller's
-- RoleId/OrganizationId/SupplierId (NULL = wildcard on that dimension) with
-- IsAllowed = 1.
--
-- The seed below only populates MenuItems, matching the current
-- DashboardLayout.tsx menuGroups exactly (dead-link items — Invoices,
-- Prices, Sales, Inventory — are intentionally left out, see CLAUDE.md debt
-- item #14). MenuAssignments is left empty on purpose: per the resolution
-- rule, that means every seeded item stays visible to everyone today,
-- preserving current behavior. Tightening visibility per role/org/supplier
-- is a follow-up done by inserting rows into MenuAssignments later.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

IF OBJECT_ID('MenuAssignments', 'U') IS NOT NULL
    DROP TABLE MenuAssignments;
GO

IF OBJECT_ID('MenuItems', 'U') IS NOT NULL
    DROP TABLE MenuItems;
GO

CREATE TABLE MenuItems
(
    MenuItemId       INT              IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MenuItemToken    UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() UNIQUE,
    ParentMenuItemId INT                  NULL,
    Name             VARCHAR(100)     NOT NULL,   -- bare i18n key suffix, e.g. "organizations" -> t("menu.organizations")
    Route            VARCHAR(250)         NULL,   -- NULL for group headers (no route of their own)
    Icon             VARCHAR(100)         NULL,   -- lookup key into the frontend's hand-drawn icon set; NULL for group headers
    SortOrder        INT              NOT NULL DEFAULT 0,
    IsActive         BIT              NOT NULL DEFAULT 1,
    CreatedUtc       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy        VARCHAR(150)         NULL,

    CONSTRAINT FK_MenuItems_ParentMenuItem FOREIGN KEY (ParentMenuItemId) REFERENCES MenuItems (MenuItemId)
);
GO

CREATE INDEX IX_MenuItems_ParentMenuItemId ON MenuItems (ParentMenuItemId);
GO

CREATE TABLE MenuAssignments
(
    MenuAssignmentId INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MenuItemId       INT           NOT NULL,
    RoleId           INT               NULL,   -- NULL = not restricted by role
    OrganizationId   INT               NULL,   -- NULL = not restricted by organization
    SupplierId       INT               NULL,   -- NULL = not restricted by supplier
    IsAllowed        BIT           NOT NULL DEFAULT 1,
    CreatedUtc       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy        VARCHAR(150)      NULL,

    CONSTRAINT FK_MenuAssignments_MenuItems      FOREIGN KEY (MenuItemId)     REFERENCES MenuItems      (MenuItemId),
    CONSTRAINT FK_MenuAssignments_Roles          FOREIGN KEY (RoleId)         REFERENCES Roles          (RoleId),
    CONSTRAINT FK_MenuAssignments_Organizations  FOREIGN KEY (OrganizationId) REFERENCES Organizations  (OrganizationId),
    CONSTRAINT FK_MenuAssignments_Suppliers      FOREIGN KEY (SupplierId)     REFERENCES Suppliers      (SupplierId)
);
GO

CREATE INDEX IX_MenuAssignments_MenuItemId     ON MenuAssignments (MenuItemId);
CREATE INDEX IX_MenuAssignments_RoleId         ON MenuAssignments (RoleId);
CREATE INDEX IX_MenuAssignments_OrganizationId ON MenuAssignments (OrganizationId);
CREATE INDEX IX_MenuAssignments_SupplierId     ON MenuAssignments (SupplierId);
GO

-- ── Seed: top-level items ────────────────────────────────────────────────
INSERT INTO MenuItems (Name, Route, Icon, SortOrder, CreatedBy) VALUES
    ('home',            '/dashboard', 'home', 1, 'System'),
    ('groupCatalogs',   NULL,         NULL,   2, 'System'),
    ('groupProcurement',NULL,         NULL,   3, 'System'),
    ('groupAdmin',      NULL,         NULL,   4, 'System');
GO

-- ── Seed: Catalogs children ──────────────────────────────────────────────
INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT MenuItemId, v.Name, v.Route, v.Icon, v.SortOrder, 'System'
FROM MenuItems p
CROSS APPLY (VALUES
    ('categories',     '/catalog/categories',      'categories',     1),
    ('families',       '/catalog/families',        'families',       2),
    ('unitTypes',      '/catalog/unit-types',       'unitTypes',      3),
    ('unitsOfMeasure', '/catalog/units-of-measure', 'unitsOfMeasure', 4)
) AS v(Name, Route, Icon, SortOrder)
WHERE p.Name = 'groupCatalogs' AND p.ParentMenuItemId IS NULL;
GO

-- ── Seed: Procurement children ───────────────────────────────────────────
INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT MenuItemId, v.Name, v.Route, v.Icon, v.SortOrder, 'System'
FROM MenuItems p
CROSS APPLY (VALUES
    ('suppliers', '/suppliers', 'suppliers', 1),
    ('articles',  '/articles',  'articles',  2)
) AS v(Name, Route, Icon, SortOrder)
WHERE p.Name = 'groupProcurement' AND p.ParentMenuItemId IS NULL;
GO

-- ── Seed: Admin children ─────────────────────────────────────────────────
INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT MenuItemId, v.Name, v.Route, v.Icon, v.SortOrder, 'System'
FROM MenuItems p
CROSS APPLY (VALUES
    ('organizations', '/organizations', 'organizations', 1),
    ('users',         '/users',         'users',         2)
) AS v(Name, Route, Icon, SortOrder)
WHERE p.Name = 'groupAdmin' AND p.ParentMenuItemId IS NULL;
GO
