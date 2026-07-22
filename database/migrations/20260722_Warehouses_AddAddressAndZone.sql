-- Adds a physical address (mirrors Suppliers'/OrganizationContacts' shape exactly) and a
-- ZoneId FK (mirrors Organizations.ZoneId's shape exactly — nullable, no CHECK constraint, no
-- owning-org-type restriction). The Warehouse, not the Organization, is what actually receives
-- deliveries, so delivery-zone coverage enforcement moves here — see CLAUDE.md's "Delivery
-- Zones" note. Idempotent/rerunnable.

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.Warehouses', 'AddressLine1') IS NULL
    ALTER TABLE Warehouses ADD AddressLine1 VARCHAR(250) NULL;
GO

IF COL_LENGTH('dbo.Warehouses', 'AddressLine2') IS NULL
    ALTER TABLE Warehouses ADD AddressLine2 VARCHAR(250) NULL;
GO

IF COL_LENGTH('dbo.Warehouses', 'City') IS NULL
    ALTER TABLE Warehouses ADD City VARCHAR(150) NULL;
GO

IF COL_LENGTH('dbo.Warehouses', 'State') IS NULL
    ALTER TABLE Warehouses ADD State VARCHAR(150) NULL;
GO

IF COL_LENGTH('dbo.Warehouses', 'PostalCode') IS NULL
    ALTER TABLE Warehouses ADD PostalCode VARCHAR(50) NULL;
GO

IF COL_LENGTH('dbo.Warehouses', 'Country') IS NULL
    ALTER TABLE Warehouses ADD Country VARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.Warehouses', 'ZoneId') IS NULL
    ALTER TABLE Warehouses ADD ZoneId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Warehouses_Zones_ZoneId')
    ALTER TABLE Warehouses ADD CONSTRAINT FK_Warehouses_Zones_ZoneId
        FOREIGN KEY (ZoneId) REFERENCES Zones (ZoneId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Warehouses_ZoneId' AND object_id = OBJECT_ID('dbo.Warehouses'))
    CREATE INDEX IX_Warehouses_ZoneId ON Warehouses (ZoneId);
GO
