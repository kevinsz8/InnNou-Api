-- =============================================================
-- MIGRATION: Create WarehouseContacts + link Users to them
-- Date: 2026-07-15
-- =============================================================
-- Every Warehouse can have N contacts (ContactName/Phone/Email/etc, same
-- shape as OrganizationContacts). Unlike OrganizationContacts, a
-- WarehouseContact can optionally get real system-login access — same
-- "shadow user" mechanism as Suppliers (see 20260701_SupplierAccess.sql):
-- every WarehouseContact gets exactly one linked Users row, whether or not
-- HasAccessToSystem is true. When false, the shadow user gets a synthetic
-- placeholder email and IsActive=0 (login blocked), but the row still
-- exists for impersonation/audit attribution.
--
-- Deliberately reuses the existing seeded EMPLOYEE role for these shadow
-- users rather than introducing a new role — the question of whether
-- warehouse contacts need a dedicated/more granular role type is an open
-- design discussion, explicitly deferred (see InnNou-Api CLAUDE.md,
-- "Warehouse contacts (shadow User)").
--
-- Unlike Suppliers (which have no Organization), a WarehouseContact's
-- shadow user IS assigned OrganizationId = the parent Warehouse's
-- OrganizationId — so the existing organization-hierarchy check inside
-- AuthService.ImpersonateAsync already governs who can impersonate them,
-- with no special superadmin-only carve-out needed (unlike Suppliers).
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('WarehouseContacts', 'U') IS NULL
BEGIN
    CREATE TABLE WarehouseContacts
    (
        WarehouseContactId    int              NOT NULL IDENTITY(1,1),
        WarehouseContactToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        WarehouseId           int              NOT NULL,

        ContactName           varchar(150)     NOT NULL,
        ContactType           varchar(100)         NULL,
        Department            varchar(100)         NULL,
        Phone                 varchar(50)          NULL,
        Mobile                varchar(50)          NULL,
        Fax                   varchar(50)          NULL,
        Email                 varchar(320)         NULL,
        Notes                 varchar(500)         NULL,
        IsPrimary             bit              NOT NULL DEFAULT (0),

        HasAccessToSystem     bit              NOT NULL DEFAULT (0),

        IsActive              bit              NOT NULL DEFAULT (1),
        IsDeleted             bit              NOT NULL DEFAULT (0),
        CreatedUtc            datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy             varchar(150)         NULL,
        LastUpdatedUtc        datetime2            NULL,
        LastUpdatedBy         varchar(150)         NULL,
        DeletedUtc            datetime2            NULL,
        DeletedBy             varchar(150)         NULL,

        CONSTRAINT PK_WarehouseContacts PRIMARY KEY (WarehouseContactId),
        CONSTRAINT FK_WarehouseContacts_Warehouses FOREIGN KEY (WarehouseId) REFERENCES Warehouses (WarehouseId)
    );

    CREATE UNIQUE INDEX UQ_WarehouseContacts_Token ON WarehouseContacts (WarehouseContactToken);
    CREATE        INDEX IX_WarehouseContacts_WarehouseId ON WarehouseContacts (WarehouseId);
END
GO

IF COL_LENGTH('Users', 'WarehouseContactId') IS NULL
BEGIN
    ALTER TABLE Users ADD WarehouseContactId int NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_WarehouseContacts')
BEGIN
    ALTER TABLE Users ADD CONSTRAINT FK_Users_WarehouseContacts
        FOREIGN KEY (WarehouseContactId) REFERENCES WarehouseContacts (WarehouseContactId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_OneUserPerWarehouseContact')
BEGIN
    CREATE UNIQUE INDEX UX_Users_OneUserPerWarehouseContact
        ON Users (WarehouseContactId) WHERE WarehouseContactId IS NOT NULL;
END
GO
