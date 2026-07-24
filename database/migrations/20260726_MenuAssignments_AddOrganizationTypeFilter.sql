-- =============================================================
-- MIGRATION: Add OrganizationTypeId filter dimension to MenuAssignments
-- Date: 2026-07-26
-- =============================================================
-- CLAUDE.md flagged this as a deliberate gap: the sidebar menu system
-- (MenuItems/MenuAssignments, sp_MenuItem_GetVisibleForContext) filters by
-- Role/Organization/Supplier only, not by OrganizationTypeCode. That was fine
-- for Orders (a Super Asociado caller still gets real read-only value from
-- that page, so showing the nav item to everyone and letting the page
-- self-gate was acceptable). It stops being acceptable for a page that is
-- SUPER_ASSOCIATE-only with zero value for an ASSOCIATE-org caller
-- (Consolidated Purchase Orders, see 20260726_MenuItems_AddConsolidatedPurchaseOrdersReport.sql)
-- — restricting only by Role would show a dead "always denied" nav item to
-- the majority of Staff+/Admin users in the system (most orgs are ASSOCIATE).
--
-- Same wildcard-NULL-per-dimension shape as RoleId/OrganizationId/SupplierId:
-- NULL = not restricted by organization type, a real Id = restricted to that
-- specific OrganizationTypes row.
--
-- Idempotent — safe to re-run.
-- =============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('MenuAssignments') AND name = 'OrganizationTypeId'
)
BEGIN
    ALTER TABLE MenuAssignments ADD OrganizationTypeId INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuAssignments_OrganizationTypes'
)
BEGIN
    ALTER TABLE MenuAssignments
        ADD CONSTRAINT FK_MenuAssignments_OrganizationTypes
        FOREIGN KEY (OrganizationTypeId) REFERENCES OrganizationTypes (OrganizationTypeId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_MenuAssignments_OrganizationTypeId'
)
BEGIN
    CREATE INDEX IX_MenuAssignments_OrganizationTypeId ON MenuAssignments (OrganizationTypeId);
END
GO

PRINT '=== Migration 20260726_MenuAssignments_AddOrganizationTypeFilter completed successfully ===';
GO
