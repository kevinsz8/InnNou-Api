-- Three table-valued-parameter types backing PurchaseOrderService.CreateGoodsReceiptAsync's new
-- batch path — GoodsReceiptLine insert, StockLevel delta application, and InventoryMovement
-- insert, each previously one round trip per accepted receipt line (up to 3 per line) inside the
-- receipt's transaction. Same TVP approach as
-- migrations/20260804_PurchaseOrderLine_CreateBatchTableType.sql, reused here for a second
-- write-heavy N+1 loop.
--
-- SQL Server has no CREATE OR ALTER / ALTER for table types — these guards make the script safe
-- to re-run, matching every other migration's re-run safety.

IF TYPE_ID(N'dbo.GoodsReceiptLineTableType') IS NULL
BEGIN
    CREATE TYPE dbo.GoodsReceiptLineTableType AS TABLE
    (
        GoodsReceiptLineToken UNIQUEIDENTIFIER NOT NULL,
        GoodsReceiptId        INT               NOT NULL,
        PurchaseOrderLineId   INT               NOT NULL,
        ArticleId             INT               NOT NULL,
        QuantityAccepted      DECIMAL(18,8)     NOT NULL,
        QuantityCourtesy      DECIMAL(18,8)     NOT NULL,
        QuantityRejected      DECIMAL(18,8)     NOT NULL,
        RejectionReason       NVARCHAR(500)     NULL,
        LotNumber             NVARCHAR(100)     NULL,
        ExpirationDate        DATE              NULL,
        SerialNumber          NVARCHAR(100)     NULL,
        Notes                 NVARCHAR(500)     NULL,
        TaxCategoryId         INT               NULL,
        TaxRateId             INT               NULL,
        TaxRatePercent        DECIMAL(11,8)     NULL,
        TaxableAmount         DECIMAL(18,8)     NULL,
        TaxAmount             DECIMAL(18,8)     NULL,
        TotalAmount           DECIMAL(18,8)     NULL
    );
END;
GO

-- Aggregated (already summed per WarehouseId+ArticleId on the C# side before this is built) so a
-- single MERGE in sp_StockLevel_ApplyDeltaBatch can safely touch each (Warehouse,Article) row
-- exactly once — MERGE errors if the same target row is matched by more than one source row.
IF TYPE_ID(N'dbo.StockLevelDeltaTableType') IS NULL
BEGIN
    CREATE TYPE dbo.StockLevelDeltaTableType AS TABLE
    (
        WarehouseId INT           NOT NULL,
        ArticleId   INT           NOT NULL,
        Delta       DECIMAL(18,8) NOT NULL
    );
END;
GO

-- Generic across every InventoryMovement origin (Receipt/Transfer/Adjustment), not just Goods
-- Receipts — @Type is the same Code column sp_InventoryMovement_Create's single-row version
-- already resolves to InventoryMovementTypeId via an inline subquery.
IF TYPE_ID(N'dbo.InventoryMovementTableType') IS NULL
BEGIN
    CREATE TYPE dbo.InventoryMovementTableType AS TABLE
    (
        InventoryMovementToken  UNIQUEIDENTIFIER NOT NULL,
        WarehouseId              INT              NOT NULL,
        ArticleId                INT              NOT NULL,
        Type                     VARCHAR(20)      NOT NULL,
        Quantity                 DECIMAL(18,8)    NOT NULL,
        GoodsReceiptLineId       INT              NULL,
        InventoryTransferLineId  INT              NULL,
        InventoryPeriodCountId   INT              NULL,
        Reason                   NVARCHAR(500)    NULL
    );
END;
GO
