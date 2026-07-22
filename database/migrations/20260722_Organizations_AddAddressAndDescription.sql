-- Adds a physical address (same shape as Suppliers/OrganizationContacts/Warehouses) and a
-- free-form Description to Organizations — purely informational/reference fields, no
-- enforcement logic tied to them (unlike Warehouse.ZoneId, which does gate delivery-zone
-- coverage). Idempotent/rerunnable.

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.Organizations', 'AddressLine1') IS NULL
    ALTER TABLE Organizations ADD AddressLine1 VARCHAR(250) NULL;
GO

IF COL_LENGTH('dbo.Organizations', 'AddressLine2') IS NULL
    ALTER TABLE Organizations ADD AddressLine2 VARCHAR(250) NULL;
GO

IF COL_LENGTH('dbo.Organizations', 'City') IS NULL
    ALTER TABLE Organizations ADD City VARCHAR(150) NULL;
GO

IF COL_LENGTH('dbo.Organizations', 'State') IS NULL
    ALTER TABLE Organizations ADD State VARCHAR(150) NULL;
GO

IF COL_LENGTH('dbo.Organizations', 'PostalCode') IS NULL
    ALTER TABLE Organizations ADD PostalCode VARCHAR(50) NULL;
GO

IF COL_LENGTH('dbo.Organizations', 'Country') IS NULL
    ALTER TABLE Organizations ADD Country VARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.Organizations', 'Description') IS NULL
    ALTER TABLE Organizations ADD Description VARCHAR(MAX) NULL;
GO
