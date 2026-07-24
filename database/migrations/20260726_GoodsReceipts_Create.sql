-- =============================================================
-- MIGRATION: Create Goods Receipts module (GoodsReceipt + GoodsReceiptLine)
-- Date: 2026-07-26
-- =============================================================
-- Closes CLAUDE.md's long-flagged "receiving/partial-receipt (waits on Goods
-- Receipts)" gap for Orders/PurchaseOrders. Record-only in V1 — no
-- stock/inventory side effects (no Inventory module exists yet); this is
-- deliberately the future Inventory module's audit source.
--
-- Same append-only, header+lines, references-not-mutates-the-PO shape already
-- used for PurchaseOrderLineRectification. Table names singular
-- (GoodsReceipt/GoodsReceiptLine) to match this specific family's own
-- convention (PurchaseOrder/PurchaseOrderLine/[Order] are singular, a
-- deliberate deviation from the rest of the codebase's plural table names).
--
-- Each GoodsReceiptLine carries a 3-way quantity split (Accepted/Courtesy/
-- Rejected) instead of a single "quantity received" — Accepted is capped
-- against the PurchaseOrderLine's effective ordered quantity (enforced in
-- the service layer, not here); Courtesy/Rejected are uncapped by design
-- (a supplier-gifted surplus or a damaged/wrong item can exceed what was
-- ordered without ever being silently counted as billable).
--
-- Idempotent — safe to re-run.
-- =============================================================

IF OBJECT_ID('GoodsReceipt', 'U') IS NULL
BEGIN
    CREATE TABLE GoodsReceipt
    (
        GoodsReceiptId     int              NOT NULL IDENTITY(1,1),
        GoodsReceiptToken  uniqueidentifier NOT NULL DEFAULT NEWID(),
        PurchaseOrderId    int              NOT NULL,
        WarehouseId        int              NOT NULL,   -- denormalized from PurchaseOrder, same pattern PurchaseOrder itself uses

        Notes              nvarchar(1000)       NULL,

        CreatedUtc         datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy          varchar(150)     NOT NULL,

        CONSTRAINT PK_GoodsReceipt PRIMARY KEY (GoodsReceiptId),
        CONSTRAINT FK_GoodsReceipt_PurchaseOrder FOREIGN KEY (PurchaseOrderId) REFERENCES PurchaseOrder (PurchaseOrderId),
        CONSTRAINT FK_GoodsReceipt_Warehouses FOREIGN KEY (WarehouseId) REFERENCES Warehouses (WarehouseId)
    );

    CREATE UNIQUE INDEX UQ_GoodsReceipt_GoodsReceiptToken ON GoodsReceipt (GoodsReceiptToken);
    CREATE INDEX IX_GoodsReceipt_PurchaseOrderId ON GoodsReceipt (PurchaseOrderId);
END
GO

IF OBJECT_ID('GoodsReceiptLine', 'U') IS NULL
BEGIN
    CREATE TABLE GoodsReceiptLine
    (
        GoodsReceiptLineId    int              NOT NULL IDENTITY(1,1),
        GoodsReceiptLineToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        GoodsReceiptId        int              NOT NULL,
        PurchaseOrderLineId   int              NOT NULL,
        ArticleId             int              NOT NULL,   -- traceability only, mirrors PurchaseOrderLine's own field

        QuantityAccepted      decimal(18,4)    NOT NULL DEFAULT (0),   -- billable, capped against remaining-to-receive (service layer)
        QuantityCourtesy      decimal(18,4)    NOT NULL DEFAULT (0),   -- supplier FOC/gift surplus, uncapped, must be explicitly flagged
        QuantityRejected      decimal(18,4)    NOT NULL DEFAULT (0),   -- damaged/wrong/short, uncapped

        RejectionReason       nvarchar(500)        NULL,
        LotNumber             nvarchar(100)        NULL,
        ExpirationDate        date                 NULL,
        SerialNumber          nvarchar(100)        NULL,
        Notes                 nvarchar(500)        NULL,

        CreatedUtc            datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy             varchar(150)     NOT NULL,

        CONSTRAINT PK_GoodsReceiptLine PRIMARY KEY (GoodsReceiptLineId),
        CONSTRAINT FK_GoodsReceiptLine_GoodsReceipt FOREIGN KEY (GoodsReceiptId) REFERENCES GoodsReceipt (GoodsReceiptId),
        CONSTRAINT FK_GoodsReceiptLine_PurchaseOrderLine FOREIGN KEY (PurchaseOrderLineId) REFERENCES PurchaseOrderLine (PurchaseOrderLineId),
        CONSTRAINT FK_GoodsReceiptLine_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId),
        CONSTRAINT CK_GoodsReceiptLine_QuantitiesNonNegative CHECK (QuantityAccepted >= 0 AND QuantityCourtesy >= 0 AND QuantityRejected >= 0),
        CONSTRAINT CK_GoodsReceiptLine_QuantitiesNotAllZero CHECK (QuantityAccepted + QuantityCourtesy + QuantityRejected > 0)
    );

    CREATE UNIQUE INDEX UQ_GoodsReceiptLine_GoodsReceiptLineToken ON GoodsReceiptLine (GoodsReceiptLineToken);
    CREATE INDEX IX_GoodsReceiptLine_GoodsReceiptId ON GoodsReceiptLine (GoodsReceiptId);

    -- Needed for the "how much has already been accepted against this PO line"
    -- aggregate the service layer computes on every new receipt.
    CREATE INDEX IX_GoodsReceiptLine_PurchaseOrderLineId ON GoodsReceiptLine (PurchaseOrderLineId);
END
GO

PRINT '=== Migration 20260726_GoodsReceipts_Create completed successfully ===';
GO
