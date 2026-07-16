-- =============================================================
-- MIGRATION: Warehouse impersonation (Users.WarehouseId + shadow User)
-- Date: 2026-07-16
-- =============================================================
-- Adds Users.WarehouseId, seeds a dedicated "Warehouse" Role
-- (RoleLevel = 10, CanImpersonate = 0 — same shape as the existing
-- "Supplier" role from 20260701_SupplierAccess.sql), backfills a
-- shadow User for every pre-existing Warehouse that doesn't have
-- one yet, and enforces UX_Users_OneUserPerWarehouse as a real
-- unique (filtered) index.
--
-- Unlike Supplier/WarehouseContact, a Warehouse shadow user is
-- ALWAYS synthetic (IsActive = 0) — there is no direct-login use
-- case for a warehouse, only impersonation, so no HasAccessToSystem
-- toggle exists here at all.
-- Guarded/idempotent — safe to re-run.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Users.WarehouseId + FK
IF COL_LENGTH('Users', 'WarehouseId') IS NULL
BEGIN
    ALTER TABLE Users ADD WarehouseId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Warehouses_WarehouseId')
BEGIN
    ALTER TABLE Users ADD CONSTRAINT FK_Users_Warehouses_WarehouseId
        FOREIGN KEY (WarehouseId) REFERENCES Warehouses (WarehouseId);
END
GO

-- 2. Seed the "Warehouse" role (RoleLevel = 10, cannot impersonate)
IF NOT EXISTS (SELECT 1 FROM Roles WHERE NormalizedName = 'WAREHOUSE')
BEGIN
    INSERT INTO Roles (RoleToken, Name, NormalizedName, Description, RoleLevel, CanImpersonate, IsActive, CreatedUtc, CreatedBy)
    VALUES (NEWID(), 'Warehouse', 'WAREHOUSE', 'Shadow/login account linked to a Warehouse record', 10, 0, 1, SYSUTCDATETIME(), 'SYSTEM-MIGRATION');
END
GO

-- 3. Backfill a shadow User for every Warehouse that doesn't have one yet.
--    PasswordHash is a fixed, valid-format bcrypt literal with an unknown/
--    discarded plaintext — safe because IsActive = 0 blocks login before
--    the hash is ever compared (see AuthService.LoginAsync).
IF EXISTS (SELECT 1 FROM Roles WHERE NormalizedName = 'WAREHOUSE')
BEGIN
    DECLARE @WarehouseRoleId INT = (SELECT RoleId FROM Roles WHERE NormalizedName = 'WAREHOUSE');

    INSERT INTO Users
    (
        UserToken, FirstName, LastName, Email, NormalizedEmail,
        UserName, NormalizedUserName, PasswordHash, RoleId,
        OrganizationId, SupplierId, WarehouseContactId, WarehouseId,
        IsActive, IsDeleted, CreatedUtc, CreatedBy
    )
    SELECT
        NEWID(),
        LEFT(w.Name, 150),
        '(Warehouse)',
        'warehouse-' + REPLACE(CONVERT(varchar(36), w.WarehouseToken), '-', '') + '@no-access.innou.internal',
        UPPER('warehouse-' + REPLACE(CONVERT(varchar(36), w.WarehouseToken), '-', '') + '@no-access.innou.internal'),
        'warehouse-' + REPLACE(CONVERT(varchar(36), w.WarehouseToken), '-', ''),
        UPPER('warehouse-' + REPLACE(CONVERT(varchar(36), w.WarehouseToken), '-', '')),
        '$2a$11$Zi.LKPFxdXtljzyqT6/KDOkjS14Kp3wmFQMI/X1uDHTszwZ3wPwmu',
        @WarehouseRoleId,
        w.OrganizationId,
        NULL,
        NULL,
        w.WarehouseId,
        0,
        0,
        SYSUTCDATETIME(),
        'SYSTEM-MIGRATION'
    FROM Warehouses w
    WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.WarehouseId = w.WarehouseId);
END
GO

-- 4. Guard against pre-existing duplicate WarehouseId links, then enforce
--    the one-warehouse-one-user invariant with a real unique filtered index.
IF EXISTS (
    SELECT WarehouseId FROM Users WHERE WarehouseId IS NOT NULL GROUP BY WarehouseId HAVING COUNT(*) > 1
)
BEGIN
    RAISERROR('Migration 20260716_Users_AddWarehouseId: duplicate Users.WarehouseId values found — resolve manually before UX_Users_OneUserPerWarehouse can be made UNIQUE.', 16, 1);
END
ELSE
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_OneUserPerWarehouse' AND object_id = OBJECT_ID('Users'))
    BEGIN
        DROP INDEX UX_Users_OneUserPerWarehouse ON Users;
    END

    CREATE UNIQUE INDEX UX_Users_OneUserPerWarehouse ON Users (WarehouseId) WHERE WarehouseId IS NOT NULL;
END
GO

PRINT '=== Migration 20260716_Users_AddWarehouseId completed successfully ===';
