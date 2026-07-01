-- =============================================================
-- MIGRATION: Supplier system access (HasAccessToSystem) + shadow User
-- Date: 2026-07-01
-- =============================================================
-- Adds Suppliers.HasAccessToSystem, seeds a "Supplier" Role
-- (RoleLevel = 10, per .claude/database-v2.md), backfills a
-- shadow User for every pre-existing Supplier that doesn't have
-- one yet, and fixes UX_Users_OneUserPerSupplier to be a real
-- unique (filtered) index instead of a plain, unenforced index.
-- Every Supplier is meant to have exactly one linked User from now
-- on (real login creds when HasAccessToSystem = 1, an inert
-- placeholder otherwise) so it can always be impersonated by a
-- superadmin regardless of whether it has real system access.
-- Guarded/idempotent — safe to re-run.
-- =============================================================

-- 1. Suppliers.HasAccessToSystem
IF COL_LENGTH('Suppliers', 'HasAccessToSystem') IS NULL
BEGIN
    ALTER TABLE Suppliers ADD HasAccessToSystem bit NOT NULL DEFAULT (0);
END
GO

-- 2. Seed the "Supplier" role (RoleLevel = 10, cannot impersonate)
IF NOT EXISTS (SELECT 1 FROM Roles WHERE NormalizedName = 'SUPPLIER')
BEGIN
    INSERT INTO Roles (RoleToken, Name, NormalizedName, Description, RoleLevel, CanImpersonate, IsActive, CreatedUtc, CreatedBy)
    VALUES (NEWID(), 'Supplier', 'SUPPLIER', 'Shadow/login account linked to a Supplier record', 10, 0, 1, SYSUTCDATETIME(), 'SYSTEM-MIGRATION');
END
GO

-- 3. Backfill a shadow User for every Supplier that doesn't have one yet.
--    PasswordHash is a fixed, valid-format bcrypt literal with an unknown/
--    discarded plaintext — safe because IsActive = 0 blocks login before
--    the hash is ever compared (see AuthService.LoginAsync).
IF EXISTS (SELECT 1 FROM Roles WHERE NormalizedName = 'SUPPLIER')
BEGIN
    DECLARE @SupplierRoleId int = (SELECT RoleId FROM Roles WHERE NormalizedName = 'SUPPLIER');

    INSERT INTO Users
    (
        UserToken, FirstName, LastName, Email, NormalizedEmail,
        UserName, NormalizedUserName, PasswordHash, RoleId,
        HotelId, SupplierId, IsActive, IsDeleted, CreatedUtc, CreatedBy
    )
    SELECT
        NEWID(),
        LEFT(s.Name, 150),
        '(Supplier Account)',
        'supplier-' + REPLACE(CONVERT(varchar(36), s.SupplierToken), '-', '') + '@no-access.innou.internal',
        UPPER('supplier-' + REPLACE(CONVERT(varchar(36), s.SupplierToken), '-', '') + '@no-access.innou.internal'),
        'supplier-' + REPLACE(CONVERT(varchar(36), s.SupplierToken), '-', ''),
        UPPER('supplier-' + REPLACE(CONVERT(varchar(36), s.SupplierToken), '-', '')),
        '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy',
        @SupplierRoleId,
        NULL,
        s.SupplierId,
        0,
        0,
        SYSUTCDATETIME(),
        'SYSTEM-MIGRATION'
    FROM Suppliers s
    WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.SupplierId = s.SupplierId);
END
GO

-- 4. Guard against pre-existing duplicate SupplierId links, then enforce
--    the one-supplier-one-user invariant with a real unique filtered index.
IF EXISTS (
    SELECT SupplierId FROM Users WHERE SupplierId IS NOT NULL GROUP BY SupplierId HAVING COUNT(*) > 1
)
BEGIN
    RAISERROR('Migration 20260701_SupplierAccess: duplicate Users.SupplierId values found — resolve manually before UX_Users_OneUserPerSupplier can be made UNIQUE.', 16, 1);
END
ELSE
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_OneUserPerSupplier' AND object_id = OBJECT_ID('Users'))
    BEGIN
        DROP INDEX UX_Users_OneUserPerSupplier ON Users;
    END

    CREATE UNIQUE INDEX UX_Users_OneUserPerSupplier ON Users (SupplierId) WHERE SupplierId IS NOT NULL;
END
GO

PRINT '=== Migration 20260701_SupplierAccess completed successfully ===';
