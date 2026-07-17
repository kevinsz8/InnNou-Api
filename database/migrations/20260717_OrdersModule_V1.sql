-- =============================================================
-- MIGRATION: Create Orders module tables (Order, OrderLine, PurchaseOrder, PurchaseOrderLine)
-- Date: 2026-07-17
-- =============================================================
-- Two-level cart -> per-supplier split model, see .claude/OrdersModule.md
-- for the full design rationale.
--
-- Order            = the cart. Organization/Warehouse-owned, can mix
--                    articles from any supplier while DRAFT.
-- OrderLine        = one row per requested article in the cart. FK to
--                    Order + Article (traceability only). Structural/price
--                    fields are snapshotted at line-add time from
--                    Article/ArticlePrice — same hybrid FK+snapshot
--                    pattern already used for Article's own structural
--                    versioning. Its responsibility ends at "what was
--                    requested" — it is never touched again once its
--                    parent Order is Submitted.
-- PurchaseOrder     = the real, supplier-facing document. Exactly one
--                    Supplier each, created automatically (one per
--                    distinct Supplier among the cart's lines) when an
--                    Order is Submitted.
-- PurchaseOrderLine = one row per OrderLine that landed in a given
--                    PurchaseOrder at split time — a full, independent
--                    snapshot copy (not a shared row with OrderLine),
--                    deliberately separate so the entire downstream
--                    lifecycle (sent, received, invoiced — future Goods
--                    Receipts fields) has its own home and its own audit
--                    trail, distinct from "who added this to the cart".
--                    Keeps an OrderLineId FK purely for traceability back
--                    to the originating cart line.
--
-- Creation order matters: Order -> OrderLine (FK to Order) ->
-- PurchaseOrder (FK to Order) -> PurchaseOrderLine (FKs to PurchaseOrder
-- and OrderLine).
--
-- Guarded so each CREATE TABLE is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('[Order]', 'U') IS NULL
BEGIN
    CREATE TABLE [Order]
    (
        OrderId          int              NOT NULL IDENTITY(1,1),
        OrderToken       uniqueidentifier NOT NULL DEFAULT NEWID(),
        OrganizationId   int              NOT NULL,
        WarehouseId      int              NOT NULL,

        Status           varchar(20)      NOT NULL DEFAULT ('DRAFT'),
        Notes            nvarchar(500)        NULL,
        SubmittedUtc     datetime2            NULL,

        CreatedUtc       datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy        varchar(150)     NOT NULL,
        LastUpdatedUtc   datetime2            NULL,
        LastUpdatedBy    varchar(150)         NULL,

        CONSTRAINT PK_Order PRIMARY KEY (OrderId),
        CONSTRAINT FK_Order_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId),
        CONSTRAINT FK_Order_Warehouses FOREIGN KEY (WarehouseId) REFERENCES Warehouses (WarehouseId),
        CONSTRAINT CK_Order_Status CHECK (Status IN (N'DRAFT', N'SUBMITTED', N'CANCELLED'))
    );

    CREATE UNIQUE INDEX UQ_Order_OrderToken ON [Order] (OrderToken);
    CREATE        INDEX IX_Order_OrganizationId ON [Order] (OrganizationId);
    CREATE        INDEX IX_Order_WarehouseId    ON [Order] (WarehouseId);
END
GO

IF OBJECT_ID('OrderLine', 'U') IS NULL
BEGIN
    CREATE TABLE OrderLine
    (
        OrderLineId        int              NOT NULL IDENTITY(1,1),
        OrderLineToken     uniqueidentifier NOT NULL DEFAULT NEWID(),
        OrderId            int              NOT NULL,
        ArticleId          int              NOT NULL,        -- traceability only, never joined for calculation

        Quantity           decimal(18,4)    NOT NULL,         -- how many PurchaseUnits requested

        -- Snapshot from Article at line-add time — frozen, never re-joined
        PurchaseUnitId     int              NOT NULL,
        PurchaseQuantity   decimal(18,4)    NOT NULL,
        ContentUnitId      int              NOT NULL,
        ContentQuantity    decimal(18,4)        NULL,

        -- Snapshot from ArticlePrice at line-add time — frozen
        UnitPrice          decimal(18,4)    NOT NULL,
        CurrencyCode       varchar(3)       NOT NULL,

        Notes              nvarchar(500)        NULL,

        CreatedUtc         datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy          varchar(150)     NOT NULL,
        LastUpdatedUtc     datetime2            NULL,
        LastUpdatedBy      varchar(150)         NULL,

        CONSTRAINT PK_OrderLine PRIMARY KEY (OrderLineId),
        CONSTRAINT FK_OrderLine_Order FOREIGN KEY (OrderId) REFERENCES [Order] (OrderId),
        CONSTRAINT FK_OrderLine_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId),
        CONSTRAINT FK_OrderLine_PurchaseUnit FOREIGN KEY (PurchaseUnitId) REFERENCES UnitsOfMeasure (UnitOfMeasureId),
        CONSTRAINT FK_OrderLine_ContentUnit FOREIGN KEY (ContentUnitId) REFERENCES UnitsOfMeasure (UnitOfMeasureId)
    );

    CREATE UNIQUE INDEX UQ_OrderLine_OrderLineToken ON OrderLine (OrderLineToken);

    -- One line per Article per Order — the upsert target for sp_OrderLine_Upsert.
    CREATE UNIQUE INDEX UX_OrderLine_Order_Article ON OrderLine (OrderId, ArticleId);

    CREATE INDEX IX_OrderLine_ArticleId ON OrderLine (ArticleId);
END
GO

IF OBJECT_ID('PurchaseOrder', 'U') IS NULL
BEGIN
    CREATE TABLE PurchaseOrder
    (
        PurchaseOrderId      int              NOT NULL IDENTITY(1,1),
        PurchaseOrderToken   uniqueidentifier NOT NULL DEFAULT NEWID(),
        OrderId              int              NOT NULL,
        SupplierId           int              NOT NULL,
        OrganizationId       int              NOT NULL,   -- denormalized from Order, direct auth queries
        WarehouseId          int              NOT NULL,   -- denormalized from Order, delivery destination

        Status               varchar(20)      NOT NULL DEFAULT ('SENT'),
        SentUtc              datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CancelledUtc         datetime2            NULL,
        CancelledBy          varchar(150)         NULL,

        CreatedUtc           datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy            varchar(150)     NOT NULL,

        CONSTRAINT PK_PurchaseOrder PRIMARY KEY (PurchaseOrderId),
        CONSTRAINT FK_PurchaseOrder_Order FOREIGN KEY (OrderId) REFERENCES [Order] (OrderId),
        CONSTRAINT FK_PurchaseOrder_Suppliers FOREIGN KEY (SupplierId) REFERENCES Suppliers (SupplierId),
        CONSTRAINT FK_PurchaseOrder_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId),
        CONSTRAINT FK_PurchaseOrder_Warehouses FOREIGN KEY (WarehouseId) REFERENCES Warehouses (WarehouseId),
        CONSTRAINT CK_PurchaseOrder_Status CHECK (Status IN (N'SENT', N'CANCELLED'))
    );

    CREATE UNIQUE INDEX UQ_PurchaseOrder_PurchaseOrderToken ON PurchaseOrder (PurchaseOrderToken);

    -- At most one PurchaseOrder per Supplier per cart — enforced at the DB
    -- layer since that's the only way it stays correct under concurrency.
    CREATE UNIQUE INDEX UX_PurchaseOrder_Order_Supplier ON PurchaseOrder (OrderId, SupplierId);

    CREATE INDEX IX_PurchaseOrder_SupplierId     ON PurchaseOrder (SupplierId);
    CREATE INDEX IX_PurchaseOrder_OrganizationId ON PurchaseOrder (OrganizationId);
END
GO

IF OBJECT_ID('PurchaseOrderLine', 'U') IS NULL
BEGIN
    CREATE TABLE PurchaseOrderLine
    (
        PurchaseOrderLineId    int              NOT NULL IDENTITY(1,1),
        PurchaseOrderLineToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        PurchaseOrderId        int              NOT NULL,
        OrderLineId            int              NOT NULL,     -- traceability back to the originating cart line
        ArticleId              int              NOT NULL,     -- traceability only, never joined for calculation

        Quantity               decimal(18,4)    NOT NULL,     -- snapshot of OrderLine.Quantity at split time

        -- Independent snapshot copy, captured at split time — deliberately
        -- not shared with OrderLine, see the header note above for why.
        PurchaseUnitId         int              NOT NULL,
        PurchaseQuantity       decimal(18,4)    NOT NULL,
        ContentUnitId          int              NOT NULL,
        ContentQuantity        decimal(18,4)        NULL,

        UnitPrice              decimal(18,4)    NOT NULL,
        CurrencyCode           varchar(3)       NOT NULL,

        Notes                  nvarchar(500)        NULL,

        CreatedUtc             datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy              varchar(150)     NOT NULL,
        LastUpdatedUtc         datetime2            NULL,
        LastUpdatedBy          varchar(150)         NULL,

        CONSTRAINT PK_PurchaseOrderLine PRIMARY KEY (PurchaseOrderLineId),
        CONSTRAINT FK_PurchaseOrderLine_PurchaseOrder FOREIGN KEY (PurchaseOrderId) REFERENCES PurchaseOrder (PurchaseOrderId),
        CONSTRAINT FK_PurchaseOrderLine_OrderLine FOREIGN KEY (OrderLineId) REFERENCES OrderLine (OrderLineId),
        CONSTRAINT FK_PurchaseOrderLine_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId),
        CONSTRAINT FK_PurchaseOrderLine_PurchaseUnit FOREIGN KEY (PurchaseUnitId) REFERENCES UnitsOfMeasure (UnitOfMeasureId),
        CONSTRAINT FK_PurchaseOrderLine_ContentUnit FOREIGN KEY (ContentUnitId) REFERENCES UnitsOfMeasure (UnitOfMeasureId)
    );

    CREATE UNIQUE INDEX UQ_PurchaseOrderLine_PurchaseOrderLineToken ON PurchaseOrderLine (PurchaseOrderLineToken);

    -- One PurchaseOrderLine per originating OrderLine — the split creates
    -- exactly one, and it's never re-created (Order is immutable once Submitted).
    CREATE UNIQUE INDEX UX_PurchaseOrderLine_OrderLineId ON PurchaseOrderLine (OrderLineId);

    CREATE INDEX IX_PurchaseOrderLine_PurchaseOrderId ON PurchaseOrderLine (PurchaseOrderId);
    CREATE INDEX IX_PurchaseOrderLine_ArticleId       ON PurchaseOrderLine (ArticleId);
END
GO
