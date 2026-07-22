-- Converts OrderApprovalSteps.Status from a CHECK-constrained varchar to an Id-backed
-- lookup table (OrderApprovalStepStatuses). See 20260722_OrderStatuses_ConvertToId.sql
-- for the full rationale — same recipe applied here.

IF OBJECT_ID('dbo.OrderApprovalStepStatuses', 'U') IS NULL
BEGIN
    CREATE TABLE OrderApprovalStepStatuses (
        OrderApprovalStepStatusId int         NOT NULL IDENTITY(1,1),
        Code                      varchar(20) NOT NULL,
        IsActive                  bit         NOT NULL DEFAULT 1,

        CONSTRAINT PK_OrderApprovalStepStatuses PRIMARY KEY (OrderApprovalStepStatusId),
        CONSTRAINT UQ_OrderApprovalStepStatuses_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# OrderApprovalStepStatus enum hardcodes these Ids
-- (Pending=1, Approved=2, Rejected=3, Cancelled=4).
IF NOT EXISTS (SELECT 1 FROM OrderApprovalStepStatuses WHERE Code = 'PENDING')
    INSERT INTO OrderApprovalStepStatuses (Code) VALUES ('PENDING');
GO
IF NOT EXISTS (SELECT 1 FROM OrderApprovalStepStatuses WHERE Code = 'APPROVED')
    INSERT INTO OrderApprovalStepStatuses (Code) VALUES ('APPROVED');
GO
IF NOT EXISTS (SELECT 1 FROM OrderApprovalStepStatuses WHERE Code = 'REJECTED')
    INSERT INTO OrderApprovalStepStatuses (Code) VALUES ('REJECTED');
GO
IF NOT EXISTS (SELECT 1 FROM OrderApprovalStepStatuses WHERE Code = 'CANCELLED')
    INSERT INTO OrderApprovalStepStatuses (Code) VALUES ('CANCELLED');
GO

IF COL_LENGTH('dbo.OrderApprovalSteps', 'OrderApprovalStepStatusId') IS NULL
    ALTER TABLE OrderApprovalSteps ADD OrderApprovalStepStatusId INT NULL;
GO

UPDATE s
    SET s.OrderApprovalStepStatusId = oass.OrderApprovalStepStatusId
FROM OrderApprovalSteps s
JOIN OrderApprovalStepStatuses oass ON oass.Code = s.Status
WHERE s.OrderApprovalStepStatusId IS NULL;
GO

IF EXISTS (SELECT 1 FROM OrderApprovalSteps WHERE OrderApprovalStepStatusId IS NULL)
    THROW 51002, 'OrderApprovalStepStatusId backfill incomplete — some OrderApprovalSteps rows have an unrecognized Status value.', 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id WHERE t.name = 'OrderApprovalSteps' AND c.name = 'OrderApprovalStepStatusId' AND c.is_nullable = 0)
    ALTER TABLE OrderApprovalSteps ALTER COLUMN OrderApprovalStepStatusId INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_OrderApprovalSteps_OrderApprovalStepStatusId')
    ALTER TABLE OrderApprovalSteps ADD CONSTRAINT DF_OrderApprovalSteps_OrderApprovalStepStatusId DEFAULT (1) FOR OrderApprovalStepStatusId; -- 1 = PENDING
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderApprovalSteps_OrderApprovalStepStatuses')
    ALTER TABLE OrderApprovalSteps ADD CONSTRAINT FK_OrderApprovalSteps_OrderApprovalStepStatuses FOREIGN KEY (OrderApprovalStepStatusId) REFERENCES OrderApprovalStepStatuses (OrderApprovalStepStatusId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderApprovalSteps_OrderApprovalStepStatusId')
    CREATE INDEX IX_OrderApprovalSteps_OrderApprovalStepStatusId ON OrderApprovalSteps (OrderApprovalStepStatusId);
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_OrderApprovalSteps_Status')
    ALTER TABLE OrderApprovalSteps DROP CONSTRAINT CK_OrderApprovalSteps_Status;
GO

DECLARE @dfName sysname;
SELECT @dfName = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.OrderApprovalSteps') AND c.name = 'Status';
IF @dfName IS NOT NULL
    EXEC('ALTER TABLE OrderApprovalSteps DROP CONSTRAINT ' + @dfName);
GO

IF COL_LENGTH('dbo.OrderApprovalSteps', 'Status') IS NOT NULL
    ALTER TABLE OrderApprovalSteps DROP COLUMN Status;
GO
