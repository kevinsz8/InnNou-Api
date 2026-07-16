-- =============================================================
-- MIGRATION: Dedicated WAREHOUSE_CONTACT role
-- Date: 2026-07-16
-- =============================================================
-- WarehouseContact shadow users previously reused the generic
-- "EMPLOYEE" role (an explicitly deferred design question — see
-- CLAUDE.md, "Warehouse contacts (shadow User)"). Now that Warehouse
-- itself has a dedicated "WAREHOUSE" role for its own shadow user
-- (20260716_Users_AddWarehouseId.sql), WarehouseContact gets the same
-- treatment: its own dedicated role, distinct from "WAREHOUSE" (a
-- contact is a person associated with a warehouse, not the warehouse
-- itself) and distinct from generic "EMPLOYEE".
--
-- Same RoleLevel/CanImpersonate as EMPLOYEE (20 / 0) — this is a
-- rename/dedication, not a capability change: WarehouseContact shadow
-- users (unlike Warehouse's own, always-synthetic shadow user) can be
-- granted real HasAccessToSystem login access and do real work, so
-- their existing capability level is preserved.
--
-- No existing Users rows reference WarehouseContactId at the time of
-- this migration (verified: 0 rows), so no backfill/reassignment is
-- needed — WarehouseContactService.CreateAsync is updated to request
-- this role for every contact going forward.
-- Guarded/idempotent — safe to re-run.
-- =============================================================

IF NOT EXISTS (SELECT 1 FROM Roles WHERE NormalizedName = 'WAREHOUSE_CONTACT')
BEGIN
    INSERT INTO Roles (RoleToken, Name, NormalizedName, Description, RoleLevel, CanImpersonate, IsActive, CreatedUtc, CreatedBy)
    VALUES (NEWID(), 'Warehouse Contact', 'WAREHOUSE_CONTACT', 'Shadow/login account linked to a WarehouseContact record', 20, 0, 1, SYSUTCDATETIME(), 'SYSTEM-MIGRATION');
END
GO

PRINT '=== Migration 20260716_WarehouseContact_DedicatedRole completed successfully ===';
