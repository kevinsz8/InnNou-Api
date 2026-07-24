-- =============================================================
-- MIGRATION: Create Inventory module (StockLevels, InventoryMovements,
--            InventoryTransfers, InventoryTransferLines)
-- Date: 2026-07-27
-- =============================================================
-- InnNou's first Inventory module. Closes the gap GoodsReceiptsModule.md
-- explicitly flagged as its own future consumer ("record-only in V1 ...
-- deliberately built as the future Inventory module's audit source"). First
-- real consumer of Warehouse.IsInventoriable/CanAdjustInventory/
-- CanTransferOut/CanReceiveTransfers (unused since 2026-07-15).
--
-- V1 scope confirmed with the user: RECEIPT (automatic, from Goods
-- Receipts) + ADJUSTMENT (manual, physical-count correction) + TRANSFER
-- (warehouse-to-warehouse, same Organization only). No CONSUMPTION yet (no
-- POS/Production driver), no valuation/cost tracking (no Accounting module).
--
-- StockLevels is a materialized balance (one row per Warehouse x Article,
-- QuantityOnHand denominated in Article.PurchaseUnitId — same unit every
-- OrderLine/PurchaseOrderLine/GoodsReceiptLine quantity already uses, zero
-- new conversion logic needed). InventoryMovements is the append-only audit
-- ledger backing it — same "materialized balance + audit trail" shape Odoo
-- uses (stock.quant + stock.move), researched before building.
--
-- Idempotent — safe to re-run.
-- =============================================================

IF OBJECT_ID('InventoryMovementTypes', 'U') IS NULL
BEGIN
    CREATE TABLE InventoryMovementTypes (
        InventoryMovementTypeId int         NOT NULL IDENTITY(1,1),
        Code                    varchar(20) NOT NULL,
        IsActive                bit         NOT NULL DEFAULT 1,

        CONSTRAINT PK_InventoryMovementTypes PRIMARY KEY (InventoryMovementTypeId),
        CONSTRAINT UQ_InventoryMovementTypes_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# InventoryMovementType enum hardcodes these Ids.
IF NOT EXISTS (SELECT 1 FROM InventoryMovementTypes WHERE Code = 'RECEIPT')
    INSERT INTO InventoryMovementTypes (Code) VALUES ('RECEIPT');
GO
IF NOT EXISTS (SELECT 1 FROM InventoryMovementTypes WHERE Code = 'ADJUSTMENT')
    INSERT INTO InventoryMovementTypes (Code) VALUES ('ADJUSTMENT');
GO
IF NOT EXISTS (SELECT 1 FROM InventoryMovementTypes WHERE Code = 'TRANSFER_OUT')
    INSERT INTO InventoryMovementTypes (Code) VALUES ('TRANSFER_OUT');
GO
IF NOT EXISTS (SELECT 1 FROM InventoryMovementTypes WHERE Code = 'TRANSFER_IN')
    INSERT INTO InventoryMovementTypes (Code) VALUES ('TRANSFER_IN');
GO

IF OBJECT_ID('StockLevels', 'U') IS NULL
BEGIN
    CREATE TABLE StockLevels (
        StockLevelId     int              NOT NULL IDENTITY(1,1),
        StockLevelToken  uniqueidentifier NOT NULL DEFAULT NEWID(),
        WarehouseId      int              NOT NULL,
        ArticleId        int              NOT NULL,

        QuantityOnHand   decimal(18,4)    NOT NULL DEFAULT (0),   -- denominated in Article.PurchaseUnitId

        CreatedUtc       datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy        varchar(150)     NOT NULL,
        LastUpdatedUtc   datetime2            NULL,
        LastUpdatedBy    varchar(150)         NULL,

        CONSTRAINT PK_StockLevels PRIMARY KEY (StockLevelId),
        CONSTRAINT FK_StockLevels_Warehouses FOREIGN KEY (WarehouseId) REFERENCES Warehouses (WarehouseId),
        CONSTRAINT FK_StockLevels_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId)
    );

    CREATE UNIQUE INDEX UQ_StockLevels_StockLevelToken ON StockLevels (StockLevelToken);

    -- One balance row per Warehouse x Article — sp_StockLevel_ApplyDelta upserts against this.
    CREATE UNIQUE INDEX UX_StockLevels_Warehouse_Article ON StockLevels (WarehouseId, ArticleId);
END
GO

IF OBJECT_ID('InventoryTransfers', 'U') IS NULL
BEGIN
    CREATE TABLE InventoryTransfers (
        InventoryTransferId     int              NOT NULL IDENTITY(1,1),
        InventoryTransferToken  uniqueidentifier NOT NULL DEFAULT NEWID(),
        FromWarehouseId         int              NOT NULL,
        ToWarehouseId           int              NOT NULL,

        Notes                   nvarchar(1000)       NULL,

        CreatedUtc              datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy               varchar(150)     NOT NULL,

        CONSTRAINT PK_InventoryTransfers PRIMARY KEY (InventoryTransferId),
        CONSTRAINT FK_InventoryTransfers_FromWarehouse FOREIGN KEY (FromWarehouseId) REFERENCES Warehouses (WarehouseId),
        CONSTRAINT FK_InventoryTransfers_ToWarehouse FOREIGN KEY (ToWarehouseId) REFERENCES Warehouses (WarehouseId),
        CONSTRAINT CK_InventoryTransfers_DifferentWarehouses CHECK (FromWarehouseId <> ToWarehouseId)
    );

    CREATE UNIQUE INDEX UQ_InventoryTransfers_InventoryTransferToken ON InventoryTransfers (InventoryTransferToken);
    CREATE INDEX IX_InventoryTransfers_FromWarehouseId ON InventoryTransfers (FromWarehouseId);
    CREATE INDEX IX_InventoryTransfers_ToWarehouseId ON InventoryTransfers (ToWarehouseId);
END
GO

IF OBJECT_ID('InventoryTransferLines', 'U') IS NULL
BEGIN
    CREATE TABLE InventoryTransferLines (
        InventoryTransferLineId    int              NOT NULL IDENTITY(1,1),
        InventoryTransferLineToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        InventoryTransferId        int              NOT NULL,
        ArticleId                  int              NOT NULL,

        Quantity                   decimal(18,4)    NOT NULL,   -- always positive, amount moved From -> To

        Notes                      nvarchar(500)        NULL,

        CreatedUtc                 datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                  varchar(150)     NOT NULL,

        CONSTRAINT PK_InventoryTransferLines PRIMARY KEY (InventoryTransferLineId),
        CONSTRAINT FK_InventoryTransferLines_InventoryTransfers FOREIGN KEY (InventoryTransferId) REFERENCES InventoryTransfers (InventoryTransferId),
        CONSTRAINT FK_InventoryTransferLines_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId),
        CONSTRAINT CK_InventoryTransferLines_QuantityPositive CHECK (Quantity > 0)
    );

    CREATE UNIQUE INDEX UQ_InventoryTransferLines_InventoryTransferLineToken ON InventoryTransferLines (InventoryTransferLineToken);
    CREATE INDEX IX_InventoryTransferLines_InventoryTransferId ON InventoryTransferLines (InventoryTransferId);
END
GO

IF OBJECT_ID('InventoryMovements', 'U') IS NULL
BEGIN
    CREATE TABLE InventoryMovements (
        InventoryMovementId      int              NOT NULL IDENTITY(1,1),
        InventoryMovementToken   uniqueidentifier NOT NULL DEFAULT NEWID(),
        WarehouseId               int              NOT NULL,
        ArticleId                 int              NOT NULL,
        InventoryMovementTypeId   int              NOT NULL,

        Quantity                  decimal(18,4)    NOT NULL,   -- signed: + increase, - decrease

        GoodsReceiptLineId        int                  NULL,   -- set only for RECEIPT
        InventoryTransferLineId   int                  NULL,   -- set only for TRANSFER_OUT/TRANSFER_IN

        Reason                    nvarchar(500)        NULL,   -- ADJUSTMENT's explanation

        CreatedUtc                datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                 varchar(150)     NOT NULL,

        CONSTRAINT PK_InventoryMovements PRIMARY KEY (InventoryMovementId),
        CONSTRAINT FK_InventoryMovements_Warehouses FOREIGN KEY (WarehouseId) REFERENCES Warehouses (WarehouseId),
        CONSTRAINT FK_InventoryMovements_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId),
        CONSTRAINT FK_InventoryMovements_InventoryMovementTypes FOREIGN KEY (InventoryMovementTypeId) REFERENCES InventoryMovementTypes (InventoryMovementTypeId),
        CONSTRAINT FK_InventoryMovements_GoodsReceiptLine FOREIGN KEY (GoodsReceiptLineId) REFERENCES GoodsReceiptLine (GoodsReceiptLineId),
        CONSTRAINT FK_InventoryMovements_InventoryTransferLines FOREIGN KEY (InventoryTransferLineId) REFERENCES InventoryTransferLines (InventoryTransferLineId),
        CONSTRAINT CK_InventoryMovements_QuantityNotZero CHECK (Quantity <> 0)
    );

    CREATE UNIQUE INDEX UQ_InventoryMovements_InventoryMovementToken ON InventoryMovements (InventoryMovementToken);
    CREATE INDEX IX_InventoryMovements_Warehouse_Article ON InventoryMovements (WarehouseId, ArticleId);
    CREATE INDEX IX_InventoryMovements_GoodsReceiptLineId ON InventoryMovements (GoodsReceiptLineId);
    CREATE INDEX IX_InventoryMovements_InventoryTransferLineId ON InventoryMovements (InventoryTransferLineId);
END
GO

PRINT '=== Migration 20260727_Inventory_Create completed successfully ===';
GO
