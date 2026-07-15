-- =============================================================
-- MIGRATION: Create Warehouses table
-- Date: 2026-07-15
-- =============================================================
-- Generic inventory-location engine: every Organization can have
-- N warehouses (General, Kitchen, Bar, Office, Transit, ...).
-- Behavior is driven entirely by the capability BIT columns below
-- (CanReceivePurchases, CanTransferOut, CanConsumeInventory, ...)
-- — application code must never branch on Name or PurposeCode.
-- PurposeCode is informational-only, used for UI filtering/reporting.
-- Guarded so it is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('Warehouses', 'U') IS NULL
BEGIN
    CREATE TABLE Warehouses
    (
        WarehouseId      int              NOT NULL IDENTITY(1,1),
        WarehouseToken   uniqueidentifier NOT NULL DEFAULT NEWID(),
        OrganizationId   int              NOT NULL,

        Name             varchar(200)     NOT NULL,
        NormalizedName   varchar(200)     NOT NULL,
        Code             varchar(50)          NULL,
        Description      varchar(500)         NULL,

        PurposeCode      varchar(30)      NOT NULL DEFAULT ('GENERAL'),

        -- Capabilities: the behavior engine. Nothing in application code should
        -- key off Name/PurposeCode — every workflow validates one of these instead.
        IsInventoriable               bit NOT NULL DEFAULT (1),
        CanReceivePurchases           bit NOT NULL DEFAULT (0),
        CanReceiveTransfers           bit NOT NULL DEFAULT (0),
        CanTransferOut                bit NOT NULL DEFAULT (0),
        CanConsumeInventory           bit NOT NULL DEFAULT (0),
        CanProduceItems               bit NOT NULL DEFAULT (0),
        CanSellItems                  bit NOT NULL DEFAULT (0),
        CanAdjustInventory            bit NOT NULL DEFAULT (0),
        CanReceiveReturns             bit NOT NULL DEFAULT (0),
        TrackLotNumbers               bit NOT NULL DEFAULT (0),
        TrackExpirationDates          bit NOT NULL DEFAULT (0),
        TrackSerialNumbers            bit NOT NULL DEFAULT (0),
        RequireApproval               bit NOT NULL DEFAULT (0),
        IsDefaultReceivingWarehouse   bit NOT NULL DEFAULT (0),
        IsDefaultConsumptionWarehouse bit NOT NULL DEFAULT (0),

        IsActive         bit              NOT NULL DEFAULT (1),
        IsDeleted        bit              NOT NULL DEFAULT (0),
        CreatedUtc       datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy        varchar(150)         NULL,
        LastUpdatedUtc   datetime2            NULL,
        LastUpdatedBy    varchar(150)         NULL,
        DeletedUtc       datetime2            NULL,
        DeletedBy        varchar(150)         NULL,

        CONSTRAINT PK_Warehouses PRIMARY KEY (WarehouseId),
        CONSTRAINT FK_Warehouses_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId),
        CONSTRAINT CK_Warehouses_PurposeCode CHECK (PurposeCode IN (
            N'GENERAL', N'STORAGE', N'KITCHEN', N'RESTAURANT', N'BAR', N'HOUSEKEEPING',
            N'MAINTENANCE', N'OFFICE', N'PRODUCTION', N'TRANSIT', N'WASTE', N'VIRTUAL', N'OTHER'
        ))
    );

    CREATE UNIQUE INDEX UQ_Warehouses_WarehouseToken ON Warehouses (WarehouseToken);
    CREATE        INDEX IX_Warehouses_OrganizationId  ON Warehouses (OrganizationId);

    CREATE UNIQUE INDEX UX_Warehouses_NormalizedName_NotDeleted
        ON Warehouses (OrganizationId, NormalizedName) WHERE IsDeleted = 0;

    CREATE UNIQUE INDEX UX_Warehouses_Code_NotDeleted
        ON Warehouses (OrganizationId, Code) WHERE IsDeleted = 0 AND Code IS NOT NULL;

    -- At most one default receiving / default consumption warehouse per organization.
    CREATE UNIQUE INDEX UX_Warehouses_DefaultReceiving
        ON Warehouses (OrganizationId) WHERE IsDefaultReceivingWarehouse = 1 AND IsDeleted = 0;

    CREATE UNIQUE INDEX UX_Warehouses_DefaultConsumption
        ON Warehouses (OrganizationId) WHERE IsDefaultConsumptionWarehouse = 1 AND IsDeleted = 0;
END
GO
