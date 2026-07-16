-- =============================================================
-- MIGRATION: Remove direct Warehouse impersonation; unify on WAREHOUSE role
-- Date: 2026-07-16
-- =============================================================
-- Reverses 20260716_Users_AddWarehouseId.sql: impersonating a warehouse
-- directly (via its own dedicated shadow User) is being removed —
-- impersonating anything warehouse-related must always go through a
-- specific WarehouseContact from now on (POST /auth/impersonate-warehouse-contact,
-- unchanged). This drops Users.WarehouseId, its shadow users, and the
-- now-unused FK/unique-index.
--
-- Also consolidates roles: the just-added "WAREHOUSE_CONTACT" role
-- (20260716_WarehouseContact_DedicatedRole.sql) is removed and
-- WarehouseContact shadow users now use the "WAREHOUSE" role instead —
-- since WAREHOUSE is no longer used for a read-mostly warehouse-only
-- identity, its RoleLevel is bumped 10 -> 20 so contacts keep the same
-- Staff-level capability they had under WAREHOUSE_CONTACT/EMPLOYEE.
-- Guarded/idempotent — safe to re-run.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('Users', 'WarehouseId') IS NOT NULL
BEGIN
    -- 1. Remove ImpersonationSessions rows referencing a warehouse shadow user
    --    (either as actor or target) before the Users rows themselves.
    DELETE s
    FROM ImpersonationSessions s
    WHERE s.ActorUserId IN (SELECT UserId FROM Users WHERE WarehouseId IS NOT NULL)
       OR s.TargetUserId IN (SELECT UserId FROM Users WHERE WarehouseId IS NOT NULL);

    -- 2. Remove the warehouse shadow users themselves.
    DELETE FROM Users WHERE WarehouseId IS NOT NULL;

    -- 3. Drop the unique index and FK, then the column.
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_OneUserPerWarehouse' AND object_id = OBJECT_ID('Users'))
    BEGIN
        DROP INDEX UX_Users_OneUserPerWarehouse ON Users;
    END

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Warehouses_WarehouseId')
    BEGIN
        ALTER TABLE Users DROP CONSTRAINT FK_Users_Warehouses_WarehouseId;
    END

    ALTER TABLE Users DROP COLUMN WarehouseId;
END
GO

-- 4. WAREHOUSE role becomes the shared role for WarehouseContact shadow users —
--    bump its RoleLevel to match what contacts need (same as EMPLOYEE/WAREHOUSE_CONTACT).
UPDATE Roles
SET RoleLevel = 20,
    Description = 'Shadow/login account linked to a WarehouseContact record'
WHERE NormalizedName = 'WAREHOUSE';
GO

-- 5. Reassign any Users row still on WAREHOUSE_CONTACT (e.g. a real contact created
--    with HasAccessToSystem = true while that role briefly existed) onto WAREHOUSE
--    before removing the role — the whole point of this migration is that WAREHOUSE
--    is now the only warehouse-side role, so nothing should be orphaned.
IF EXISTS (SELECT 1 FROM Roles WHERE NormalizedName = 'WAREHOUSE_CONTACT')
BEGIN
    DECLARE @WarehouseRoleId INT = (SELECT RoleId FROM Roles WHERE NormalizedName = 'WAREHOUSE');
    DECLARE @WarehouseContactRoleId INT = (SELECT RoleId FROM Roles WHERE NormalizedName = 'WAREHOUSE_CONTACT');

    UPDATE Users SET RoleId = @WarehouseRoleId WHERE RoleId = @WarehouseContactRoleId;

    DELETE FROM Roles WHERE RoleId = @WarehouseContactRoleId;
END
GO

-- 6. Drop the now-unused stored procedure.
DROP PROCEDURE IF EXISTS dbo.sp_Auth_GetUserByWarehouseToken;
GO

PRINT '=== Migration 20260716_RemoveDirectWarehouseImpersonation completed successfully ===';
