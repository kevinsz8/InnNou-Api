-- Adds dbo.PurchaseOrderLineTableType, a table-valued parameter type used by the new
-- sp_PurchaseOrderLine_CreateBatch (see stored-procedures/PurchaseOrderLine/) to insert every
-- PurchaseOrderLine for a supplier's split in one round trip instead of one
-- sp_PurchaseOrderLine_Create call per OrderLine — the first N+1 write-loop fix in this codebase
-- to use a table-valued parameter rather than the STRING_SPLIT convention used everywhere else
-- (STRING_SPLIT only carries scalar lists, not full per-row payloads). Column list mirrors
-- sp_PurchaseOrderLine_Create's own parameter list exactly, minus @CreatedBy (shared across the
-- whole batch, passed as its own scalar parameter rather than repeated per row).
--
-- SQL Server has no CREATE OR ALTER for table types (and no ALTER TYPE at all) — this guard makes
-- the script safe to re-run, matching every other migration's re-run safety.
IF TYPE_ID(N'dbo.PurchaseOrderLineTableType') IS NULL
BEGIN
    CREATE TYPE dbo.PurchaseOrderLineTableType AS TABLE
    (
        PurchaseOrderLineToken UNIQUEIDENTIFIER NOT NULL,
        PurchaseOrderId        INT               NOT NULL,
        OrderLineId            INT               NULL,
        ArticleId              INT               NOT NULL,
        Quantity                DECIMAL(18,8)    NOT NULL,
        PurchaseUnitId          INT              NOT NULL,
        PurchaseQuantity        DECIMAL(18,8)    NOT NULL,
        ContentUnitId           INT              NOT NULL,
        ContentQuantity         DECIMAL(18,8)    NULL,
        UnitPrice               DECIMAL(18,8)    NOT NULL,
        CurrencyCode            VARCHAR(3)       NOT NULL,
        CategoryId              INT              NULL,
        CategoryCode            NVARCHAR(50)     NULL,
        SubCategoryId           INT              NULL,
        SubCategoryCode         NVARCHAR(50)     NULL,
        Notes                   NVARCHAR(500)    NULL
    );
END;
GO
