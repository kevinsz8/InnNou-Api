-- Converts Order.Status from a CHECK-constrained varchar to an Id-backed lookup table
-- (OrderStatuses), mirroring the OrganizationTypes precedent (see CLAUDE.md's
-- "Status fields: use Id not text" note). Idempotent/rerunnable, same style as
-- every other migration in this codebase.

IF OBJECT_ID('dbo.OrderStatuses', 'U') IS NULL
BEGIN
    CREATE TABLE OrderStatuses (
        OrderStatusId int         NOT NULL IDENTITY(1,1),
        Code          varchar(20) NOT NULL,
        IsActive      bit         NOT NULL DEFAULT 1,

        CONSTRAINT PK_OrderStatuses PRIMARY KEY (OrderStatusId),
        CONSTRAINT UQ_OrderStatuses_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# OrderStatus enum hardcodes these Ids (Draft=1,
-- PendingApproval=2, Submitted=3, Cancelled=4).
IF NOT EXISTS (SELECT 1 FROM OrderStatuses WHERE Code = 'DRAFT')
    INSERT INTO OrderStatuses (Code) VALUES ('DRAFT');
GO
IF NOT EXISTS (SELECT 1 FROM OrderStatuses WHERE Code = 'PENDING_APPROVAL')
    INSERT INTO OrderStatuses (Code) VALUES ('PENDING_APPROVAL');
GO
IF NOT EXISTS (SELECT 1 FROM OrderStatuses WHERE Code = 'SUBMITTED')
    INSERT INTO OrderStatuses (Code) VALUES ('SUBMITTED');
GO
IF NOT EXISTS (SELECT 1 FROM OrderStatuses WHERE Code = 'CANCELLED')
    INSERT INTO OrderStatuses (Code) VALUES ('CANCELLED');
GO

IF COL_LENGTH('dbo.[Order]', 'OrderStatusId') IS NULL
    ALTER TABLE [Order] ADD OrderStatusId INT NULL;
GO

UPDATE o
    SET o.OrderStatusId = os.OrderStatusId
FROM [Order] o
JOIN OrderStatuses os ON os.Code = o.Status
WHERE o.OrderStatusId IS NULL;
GO

IF EXISTS (SELECT 1 FROM [Order] WHERE OrderStatusId IS NULL)
    THROW 51000, 'OrderStatusId backfill incomplete — some Order rows have an unrecognized Status value.', 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id WHERE t.name = 'Order' AND c.name = 'OrderStatusId' AND c.is_nullable = 0)
    ALTER TABLE [Order] ALTER COLUMN OrderStatusId INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Order_OrderStatusId')
    ALTER TABLE [Order] ADD CONSTRAINT DF_Order_OrderStatusId DEFAULT (1) FOR OrderStatusId; -- 1 = DRAFT
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Order_OrderStatuses')
    ALTER TABLE [Order] ADD CONSTRAINT FK_Order_OrderStatuses FOREIGN KEY (OrderStatusId) REFERENCES OrderStatuses (OrderStatusId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Order_OrderStatusId')
    CREATE INDEX IX_Order_OrderStatusId ON [Order] (OrderStatusId);
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Order_Status')
    ALTER TABLE [Order] DROP CONSTRAINT CK_Order_Status;
GO

-- The original migration's column default ('DRAFT') was never explicitly named, so
-- SQL Server auto-generated a name (e.g. DF__Order__Status__...) — find and drop it
-- dynamically before dropping the column itself.
DECLARE @dfName sysname;
SELECT @dfName = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.[Order]') AND c.name = 'Status';
IF @dfName IS NOT NULL
    EXEC('ALTER TABLE [Order] DROP CONSTRAINT ' + @dfName);
GO

IF COL_LENGTH('dbo.[Order]', 'Status') IS NOT NULL
    ALTER TABLE [Order] DROP COLUMN Status;
GO
