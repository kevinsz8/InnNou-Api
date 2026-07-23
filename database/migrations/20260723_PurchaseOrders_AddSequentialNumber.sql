SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: PurchaseOrder sequential human-readable numbers
   Format PO-{Year}-{5-digit zero-padded number}, e.g. PO-2026-00042.
   Scoped per Organization (each Asociado's own sequence starts at 1,
   never shared across tenants) and per calendar year (resets each
   January 1st — the Year segment is the year the PurchaseOrder was
   created/sent, never recomputed later).

   PurchaseOrderNumberCounters is the atomic-increment source of truth
   (see sp_PurchaseOrder_Create for the concurrency-safe
   UPDATE-then-INSERT-with-duplicate-key-retry pattern, same convention
   as sp_IdempotencyKey_TryBegin's 2601/2627 catch). PurchaseOrder.
   PurchaseOrderNumber itself is a frozen, never-recomputed display
   value — not derived on read.

   Idempotent — safe to re-run.
   ============================================================= */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PurchaseOrderNumberCounters')
BEGIN
    CREATE TABLE PurchaseOrderNumberCounters
    (
        PurchaseOrderNumberCounterId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OrganizationId               INT                NOT NULL,
        Year                         INT                NOT NULL,
        LastNumber                   INT                NOT NULL DEFAULT (0),

        CONSTRAINT FK_PurchaseOrderNumberCounters_Organizations
            FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PurchaseOrderNumberCounters_Org_Year' AND object_id = OBJECT_ID('PurchaseOrderNumberCounters'))
BEGIN
    CREATE UNIQUE INDEX UX_PurchaseOrderNumberCounters_Org_Year ON PurchaseOrderNumberCounters (OrganizationId, Year);
END
GO

IF COL_LENGTH('dbo.PurchaseOrder', 'PurchaseOrderNumber') IS NULL
    ALTER TABLE PurchaseOrder ADD PurchaseOrderNumber VARCHAR(20) NULL;
GO

-- Backfill existing rows: assign each Organization+Year group a sequence ordered by CreatedUtc,
-- so already-sent purchase orders get a real, chronologically-consistent number instead of a
-- placeholder.
;WITH Ordered AS (
    SELECT
        PurchaseOrderId,
        OrganizationId,
        YEAR(CreatedUtc) AS OrderYear,
        ROW_NUMBER() OVER (PARTITION BY OrganizationId, YEAR(CreatedUtc) ORDER BY CreatedUtc, PurchaseOrderId) AS RowNum
    FROM PurchaseOrder
    WHERE PurchaseOrderNumber IS NULL
)
UPDATE po
    SET po.PurchaseOrderNumber = 'PO-' + CAST(o.OrderYear AS VARCHAR(4)) + '-' + RIGHT('00000' + CAST(o.RowNum AS VARCHAR(10)), 5)
FROM PurchaseOrder po
JOIN Ordered o ON o.PurchaseOrderId = po.PurchaseOrderId;
GO

-- Seed the counters so a NEW PurchaseOrder's number continues right after the backfilled ones —
-- never restarts from 1 and never collides with an existing number.
INSERT INTO PurchaseOrderNumberCounters (OrganizationId, Year, LastNumber)
SELECT po.OrganizationId, YEAR(po.CreatedUtc), COUNT(*)
FROM PurchaseOrder po
WHERE NOT EXISTS (
    SELECT 1 FROM PurchaseOrderNumberCounters c
    WHERE c.OrganizationId = po.OrganizationId AND c.Year = YEAR(po.CreatedUtc)
)
GROUP BY po.OrganizationId, YEAR(po.CreatedUtc);
GO

IF EXISTS (SELECT 1 FROM PurchaseOrder WHERE PurchaseOrderNumber IS NULL)
    THROW 51003, 'PurchaseOrderNumber backfill incomplete.', 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id WHERE t.name = 'PurchaseOrder' AND c.name = 'PurchaseOrderNumber' AND c.is_nullable = 0)
    ALTER TABLE PurchaseOrder ALTER COLUMN PurchaseOrderNumber VARCHAR(20) NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PurchaseOrder_Org_Number' AND object_id = OBJECT_ID('PurchaseOrder'))
BEGIN
    CREATE UNIQUE INDEX UX_PurchaseOrder_Org_Number ON PurchaseOrder (OrganizationId, PurchaseOrderNumber);
END
GO

PRINT '=== Migration 20260723_PurchaseOrders_AddSequentialNumber completed successfully ===';
GO
