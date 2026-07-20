-- =============================================================
-- MIGRATION: Delivery Zones — Countries, Zones, Organizations.ZoneId,
-- SupplierDeliveryZones
-- Date: 2026-07-21
-- =============================================================
-- A Zone (e.g. "Barcelona") belongs to exactly one Country — 2-level
-- geography, no Region/State intermediate level. Only ASSOCIATE-type
-- Organizations ever get a ZoneId (app-layer enforced, see
-- OrganizationService) — Super Asociado orgs are never zoned and are
-- never filtered by zone anywhere.
--
-- SupplierDeliveryZones is the explicit (Supplier, Zone, DayOfWeek)
-- coverage dataset — a supplier being "in Spain" never implies
-- coverage of any Spanish zone; every combination must be its own row.
-- Mirrors ArticleFavorites' shape exactly: plain existence/toggle join
-- row, hard delete, no IsActive/IsDeleted/LastUpdated*. DayOfWeek uses
-- System.DayOfWeek's convention (0=Sunday..6=Saturday) — never SQL
-- Server's @@DATEFIRST-dependent DATEPART(WEEKDAY,...); this column is
-- only ever written/read from C#.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Countries: minimal seeded lookup, same shape as Currencies (no CRUD,
-- no token, no audit columns).
IF OBJECT_ID('Countries', 'U') IS NULL
BEGIN
    CREATE TABLE Countries
    (
        CountryId INT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code      VARCHAR(2)   NOT NULL UNIQUE,   -- ISO 3166-1 alpha-2, e.g. ES, US
        Name      VARCHAR(150) NOT NULL,
        IsActive  BIT          NOT NULL DEFAULT 1
    );
END
GO

-- Zones: flat catalog, Admin+ only, no ownership-splitting. Code unique
-- per Country; CountryId is immutable after create.
IF OBJECT_ID('Zones', 'U') IS NULL
BEGIN
    CREATE TABLE Zones
    (
        ZoneId         INT              IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ZoneToken      UNIQUEIDENTIFIER NOT NULL UNIQUE DEFAULT NEWID(),
        CountryId      INT              NOT NULL,
        Code           VARCHAR(50)      NOT NULL,
        Name           VARCHAR(150)     NOT NULL,
        IsActive       BIT              NOT NULL DEFAULT 1,
        CreatedUtc     DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy      VARCHAR(150)     NOT NULL,
        LastUpdatedUtc DATETIME2        NULL,
        LastUpdatedBy  VARCHAR(150)     NULL,
        CONSTRAINT FK_Zones_Countries FOREIGN KEY (CountryId) REFERENCES Countries (CountryId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Zones_Country_Code' AND object_id = OBJECT_ID('Zones'))
BEGIN
    CREATE UNIQUE INDEX UX_Zones_Country_Code ON Zones (CountryId, Code);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Zones_CountryId' AND object_id = OBJECT_ID('Zones'))
BEGIN
    CREATE INDEX IX_Zones_CountryId ON Zones (CountryId);
END
GO

-- Organizations.ZoneId — nullable; app-layer enforced to only ever be set
-- for ASSOCIATE-type orgs. NULL = "zone not enforced yet", the deliberate
-- escape hatch used by both the Order-line availability gate and the
-- supplier-selector filter.
IF COL_LENGTH('Organizations', 'ZoneId') IS NULL
BEGIN
    ALTER TABLE Organizations ADD ZoneId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Organizations_Zones_ZoneId')
BEGIN
    ALTER TABLE Organizations ADD CONSTRAINT FK_Organizations_Zones_ZoneId
        FOREIGN KEY (ZoneId) REFERENCES Zones (ZoneId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Organizations_ZoneId' AND object_id = OBJECT_ID('Organizations'))
BEGIN
    CREATE INDEX IX_Organizations_ZoneId ON Organizations (ZoneId);
END
GO

-- SupplierDeliveryZones: explicit (Supplier, Zone, DayOfWeek) coverage rows.
IF OBJECT_ID('SupplierDeliveryZones', 'U') IS NULL
BEGIN
    CREATE TABLE SupplierDeliveryZones
    (
        SupplierDeliveryZoneId    INT              IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierDeliveryZoneToken UNIQUEIDENTIFIER NOT NULL UNIQUE DEFAULT NEWID(),
        SupplierId                INT              NOT NULL,
        ZoneId                    INT              NOT NULL,
        DayOfWeek                 TINYINT          NOT NULL,
        CreatedUtc                DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                 VARCHAR(150)     NOT NULL,
        CONSTRAINT FK_SupplierDeliveryZones_Suppliers FOREIGN KEY (SupplierId) REFERENCES Suppliers (SupplierId),
        CONSTRAINT FK_SupplierDeliveryZones_Zones      FOREIGN KEY (ZoneId)     REFERENCES Zones (ZoneId),
        CONSTRAINT CK_SupplierDeliveryZones_DayOfWeek   CHECK (DayOfWeek BETWEEN 0 AND 6)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_SupplierDeliveryZones_Supplier_Zone_Day' AND object_id = OBJECT_ID('SupplierDeliveryZones'))
BEGIN
    CREATE UNIQUE INDEX UX_SupplierDeliveryZones_Supplier_Zone_Day ON SupplierDeliveryZones (SupplierId, ZoneId, DayOfWeek);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierDeliveryZones_ZoneId' AND object_id = OBJECT_ID('SupplierDeliveryZones'))
BEGIN
    CREATE INDEX IX_SupplierDeliveryZones_ZoneId ON SupplierDeliveryZones (ZoneId);
END
GO

PRINT '=== Migration 20260721_DeliveryZones_Create completed successfully ===';
GO
