-- Converts PurchaseOrder.Status from a CHECK-constrained varchar to an Id-backed lookup
-- table (PurchaseOrderStatuses). See 20260722_OrderStatuses_ConvertToId.sql for the
-- full rationale — same recipe applied here.

IF OBJECT_ID('dbo.PurchaseOrderStatuses', 'U') IS NULL
BEGIN
    CREATE TABLE PurchaseOrderStatuses (
        PurchaseOrderStatusId int         NOT NULL IDENTITY(1,1),
        Code                  varchar(20) NOT NULL,
        IsActive              bit         NOT NULL DEFAULT 1,

        CONSTRAINT PK_PurchaseOrderStatuses PRIMARY KEY (PurchaseOrderStatusId),
        CONSTRAINT UQ_PurchaseOrderStatuses_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# PurchaseOrderStatus enum hardcodes these Ids (Sent=1, Cancelled=2).
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderStatuses WHERE Code = 'SENT')
    INSERT INTO PurchaseOrderStatuses (Code) VALUES ('SENT');
GO
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderStatuses WHERE Code = 'CANCELLED')
    INSERT INTO PurchaseOrderStatuses (Code) VALUES ('CANCELLED');
GO

IF COL_LENGTH('dbo.PurchaseOrder', 'PurchaseOrderStatusId') IS NULL
    ALTER TABLE PurchaseOrder ADD PurchaseOrderStatusId INT NULL;
GO

UPDATE po
    SET po.PurchaseOrderStatusId = pos.PurchaseOrderStatusId
FROM PurchaseOrder po
JOIN PurchaseOrderStatuses pos ON pos.Code = po.Status
WHERE po.PurchaseOrderStatusId IS NULL;
GO

IF EXISTS (SELECT 1 FROM PurchaseOrder WHERE PurchaseOrderStatusId IS NULL)
    THROW 51001, 'PurchaseOrderStatusId backfill incomplete — some PurchaseOrder rows have an unrecognized Status value.', 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id WHERE t.name = 'PurchaseOrder' AND c.name = 'PurchaseOrderStatusId' AND c.is_nullable = 0)
    ALTER TABLE PurchaseOrder ALTER COLUMN PurchaseOrderStatusId INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_PurchaseOrder_PurchaseOrderStatusId')
    ALTER TABLE PurchaseOrder ADD CONSTRAINT DF_PurchaseOrder_PurchaseOrderStatusId DEFAULT (1) FOR PurchaseOrderStatusId; -- 1 = SENT
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PurchaseOrder_PurchaseOrderStatuses')
    ALTER TABLE PurchaseOrder ADD CONSTRAINT FK_PurchaseOrder_PurchaseOrderStatuses FOREIGN KEY (PurchaseOrderStatusId) REFERENCES PurchaseOrderStatuses (PurchaseOrderStatusId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PurchaseOrder_PurchaseOrderStatusId')
    CREATE INDEX IX_PurchaseOrder_PurchaseOrderStatusId ON PurchaseOrder (PurchaseOrderStatusId);
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PurchaseOrder_Status')
    ALTER TABLE PurchaseOrder DROP CONSTRAINT CK_PurchaseOrder_Status;
GO

DECLARE @dfName sysname;
SELECT @dfName = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.PurchaseOrder') AND c.name = 'Status';
IF @dfName IS NOT NULL
    EXEC('ALTER TABLE PurchaseOrder DROP CONSTRAINT ' + @dfName);
GO

IF COL_LENGTH('dbo.PurchaseOrder', 'Status') IS NOT NULL
    ALTER TABLE PurchaseOrder DROP COLUMN Status;
GO
