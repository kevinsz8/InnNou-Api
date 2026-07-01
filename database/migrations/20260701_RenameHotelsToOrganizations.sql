-- ============================================================================
-- Migration: Rename Hotel -> Organization, add OrganizationTypes
-- Date: 2026-07-01
-- ============================================================================
-- Purpose:
--   The "Hotel" concept is being renamed to "Organization" everywhere. The
--   existing Super Associate / Associate distinction (previously implied by
--   ParentHotelId IS NULL vs NOT NULL) is formalized as an explicit
--   OrganizationTypeId FK to a new small seeded lookup table, OrganizationTypes,
--   so future org types can be added without further hierarchy heuristics.
--
--   Per project decision (early-stage, no relevant data to preserve in these
--   tables), Hotels / HotelContacts / HotelSuppliers are dropped and recreated
--   as Organizations / OrganizationContacts / OrganizationSuppliers rather than
--   renamed in place. Users.HotelId is renamed in place (via sp_rename) since
--   Users holds real login accounts that must be preserved.
--
-- IMPORTANT: Run this AFTER 20260701_Article_AddBaseUnitId.sql and
--   20260701_SupplierAccess.sql, regardless of filename date-sort order -- it
--   depends on the Hotels/HotelContacts/HotelSuppliers/Users schema those
--   migrations (and all prior ones) already established.
--
-- Idempotency: table/FK/index drops and creates are NOT re-runnable as-is
--   (unlike the seed-only migrations before it) -- this is a one-time
--   structural migration. The OrganizationTypes seed rows ARE idempotent.
-- ============================================================================

SET NOCOUNT ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

-- ----------------------------------------------------------------------------
-- 1. OrganizationTypes (new minimal seeded lookup -- no CRUD service, just
--    table + seed; the Code is denormalized into Organization reads via JOIN)
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.OrganizationTypes', 'U') IS NULL
BEGIN
    CREATE TABLE OrganizationTypes (
        OrganizationTypeId int            NOT NULL IDENTITY(1,1),
        Code               varchar(50)    NOT NULL,
        IsActive           bit            NOT NULL DEFAULT 1,

        CONSTRAINT PK_OrganizationTypes   PRIMARY KEY (OrganizationTypeId),
        CONSTRAINT UQ_OrganizationTypes_Code UNIQUE (Code)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM OrganizationTypes WHERE Code = 'SUPER_ASSOCIATE')
BEGIN
    INSERT INTO OrganizationTypes (Code, IsActive) VALUES ('SUPER_ASSOCIATE', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM OrganizationTypes WHERE Code = 'ASSOCIATE')
BEGIN
    INSERT INTO OrganizationTypes (Code, IsActive) VALUES ('ASSOCIATE', 1);
END
GO

-- ----------------------------------------------------------------------------
-- 2. Drop FKs into Hotels, then drop HotelContacts / HotelSuppliers / Hotels
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.FK_Users_Hotels', 'F') IS NOT NULL
    ALTER TABLE Users DROP CONSTRAINT FK_Users_Hotels;
GO

IF OBJECT_ID('dbo.HotelContacts', 'U') IS NOT NULL
    DROP TABLE HotelContacts;
GO

IF OBJECT_ID('dbo.HotelSuppliers', 'U') IS NOT NULL
    DROP TABLE HotelSuppliers;
GO

IF OBJECT_ID('dbo.Hotels', 'U') IS NOT NULL
    DROP TABLE Hotels;
GO

-- ----------------------------------------------------------------------------
-- 3. Organizations  (was Hotels; self-referencing via ParentOrganizationId;
--    new OrganizationTypeId FK)
-- ----------------------------------------------------------------------------
CREATE TABLE Organizations (
    OrganizationId     int              NOT NULL IDENTITY(1,1),
    OrganizationToken  uniqueidentifier NOT NULL DEFAULT NEWID(),
    Name                varchar(200)     NOT NULL,
    NormalizedName      varchar(200)     NOT NULL,
    LegalName           varchar(250)         NULL,
    Code                varchar(50)          NULL,
    ParentOrganizationId int                 NULL,
    OrganizationTypeId  int              NOT NULL,
    TimeZone             varchar(100)        NULL,
    CurrencyCode         varchar(10)         NULL,
    LanguageCode         varchar(10)         NULL,
    IsActive             bit              NOT NULL DEFAULT 1,
    IsDeleted            bit              NOT NULL DEFAULT 0,
    CreatedUtc           datetime2        NOT NULL,
    CreatedBy            varchar(150)         NULL,
    LastUpdatedUtc        datetime2           NULL,
    LastUpdatedBy         varchar(150)        NULL,
    DeletedUtc            datetime2           NULL,
    DeletedBy             varchar(150)        NULL,

    CONSTRAINT PK_Organizations                 PRIMARY KEY (OrganizationId),
    CONSTRAINT FK_Organizations_ParentOrganization FOREIGN KEY (ParentOrganizationId) REFERENCES Organizations (OrganizationId),
    CONSTRAINT FK_Organizations_OrganizationTypes  FOREIGN KEY (OrganizationTypeId)   REFERENCES OrganizationTypes (OrganizationTypeId)
);
GO

CREATE UNIQUE INDEX UQ_Organizations_OrganizationToken      ON Organizations (OrganizationToken);
CREATE        INDEX IX_Organizations_ParentOrganizationId   ON Organizations (ParentOrganizationId);
CREATE        INDEX IX_Organizations_OrganizationTypeId     ON Organizations (OrganizationTypeId);
CREATE UNIQUE INDEX UX_Organizations_NormalizedName_NotDeleted ON Organizations (NormalizedName) WHERE IsDeleted = 0;
GO

-- ----------------------------------------------------------------------------
-- 4. OrganizationContacts  (was HotelContacts; depends on Organizations)
-- ----------------------------------------------------------------------------
CREATE TABLE OrganizationContacts (
    OrganizationContactId    int              NOT NULL IDENTITY(1,1),
    OrganizationContactToken uniqueidentifier NOT NULL DEFAULT NEWID(),
    OrganizationId           int              NOT NULL,
    ContactName              varchar(150)     NOT NULL,
    ContactType              varchar(100)         NULL,
    Department               varchar(100)         NULL,
    Phone                    varchar(50)          NULL,
    Mobile                   varchar(50)          NULL,
    Fax                      varchar(50)          NULL,
    Email                    varchar(320)         NULL,
    Notes                    varchar(500)         NULL,
    IsPrimary                bit              NOT NULL DEFAULT 0,
    IsActive                 bit              NOT NULL DEFAULT 1,
    IsDeleted                bit              NOT NULL DEFAULT 0,
    CreatedUtc               datetime2        NOT NULL,
    CreatedBy                varchar(150)         NULL,
    LastUpdatedUtc            datetime2           NULL,
    LastUpdatedBy             varchar(150)        NULL,
    DeletedUtc                datetime2           NULL,
    DeletedBy                 varchar(150)        NULL,

    CONSTRAINT PK_OrganizationContacts        PRIMARY KEY (OrganizationContactId),
    CONSTRAINT FK_OrganizationContacts_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId)
);
GO

CREATE UNIQUE INDEX UQ_OrganizationContacts_Token ON OrganizationContacts (OrganizationContactToken);
GO

-- ----------------------------------------------------------------------------
-- 5. OrganizationSuppliers  (was HotelSuppliers; depends on Organizations, Suppliers)
-- ----------------------------------------------------------------------------
CREATE TABLE OrganizationSuppliers (
    OrganizationSupplierId int          NOT NULL IDENTITY(1,1),
    OrganizationId         int          NOT NULL,
    SupplierId              int          NOT NULL,
    IsActive                 bit          NOT NULL DEFAULT 1,
    CreatedUtc                datetime2    NOT NULL,
    CreatedBy                 varchar(150)     NULL,
    LastUpdatedUtc              datetime2       NULL,
    LastUpdatedBy                varchar(150)    NULL,

    CONSTRAINT PK_OrganizationSuppliers            PRIMARY KEY (OrganizationSupplierId),
    CONSTRAINT FK_OrganizationSuppliers_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId),
    CONSTRAINT FK_OrganizationSuppliers_Suppliers     FOREIGN KEY (SupplierId)     REFERENCES Suppliers (SupplierId)
);
GO

CREATE UNIQUE INDEX UQ_OrganizationSuppliers_Organization_Supplier ON OrganizationSuppliers (OrganizationId, SupplierId);
CREATE        INDEX IX_OrganizationSuppliers_OrganizationId        ON OrganizationSuppliers (OrganizationId);
CREATE        INDEX IX_OrganizationSuppliers_SupplierId            ON OrganizationSuppliers (SupplierId);
GO

-- ----------------------------------------------------------------------------
-- 6. Users.HotelId -> Users.OrganizationId (rename in place -- preserves data)
--    CK_Users_OnlyOneScope and IX_Users_HotelId both reference the column, so
--    sp_rename fails with error 15336 ("participates in enforced dependencies")
--    unless they're dropped first and recreated against the new name after.
-- ----------------------------------------------------------------------------
DROP INDEX IX_Users_HotelId ON dbo.Users;
GO

ALTER TABLE Users DROP CONSTRAINT CK_Users_OnlyOneScope;
GO

EXEC sp_rename 'Users.HotelId', 'OrganizationId', 'COLUMN';
GO

-- Any existing Users.OrganizationId (formerly HotelId) values pointing at the
-- now-dropped Hotels rows are orphaned -- Organizations was recreated with
-- fresh identity values, so there is no way to remap the old ids. Cleared to
-- NULL rather than left dangling; reassign affected users via the new
-- /organizations API once real Organization rows exist.
UPDATE Users SET OrganizationId = NULL WHERE OrganizationId IS NOT NULL;
GO

CREATE INDEX IX_Users_OrganizationId ON dbo.Users (OrganizationId);
GO

ALTER TABLE Users
    ADD CONSTRAINT FK_Users_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId);
GO

ALTER TABLE Users
    ADD CONSTRAINT CK_Users_OnlyOneScope CHECK
    (
        [OrganizationId] IS NOT NULL AND [SupplierId] IS NULL
        OR [OrganizationId] IS NULL AND [SupplierId] IS NOT NULL
        OR [OrganizationId] IS NULL AND [SupplierId] IS NULL
    );
GO

PRINT '=== Migration 20260701_RenameHotelsToOrganizations completed successfully ===';
GO
