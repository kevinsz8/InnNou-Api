-- =============================================================
-- MIGRATION: Add IsMainWarehouse capability flag to Warehouses
-- Date: 2026-07-22
-- =============================================================
-- IsMainWarehouse (EsAlmacenCentral) marks a warehouse as the
-- organization's central/main warehouse. Same "at most one per
-- organization" shape as IsDefaultReceivingWarehouse/
-- IsDefaultConsumptionWarehouse — enforced with a filtered unique
-- index rather than app-level locking, so it stays correct under
-- concurrent writes. Guarded so it is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Warehouses') AND name = 'IsMainWarehouse')
BEGIN
    ALTER TABLE Warehouses ADD IsMainWarehouse bit NOT NULL DEFAULT (0);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Warehouses') AND name = 'UX_Warehouses_Main')
BEGIN
    CREATE UNIQUE INDEX UX_Warehouses_Main
        ON Warehouses (OrganizationId) WHERE IsMainWarehouse = 1 AND IsDeleted = 0;
END
GO
