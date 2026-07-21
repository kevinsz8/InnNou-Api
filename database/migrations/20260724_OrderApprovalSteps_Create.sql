SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: OrderApprovalSteps (per-Order approval execution/audit trail)
   Created when OrderService.SubmitAsync detects a Family total crossing a
   configured FamilyApprovalThreshold — one row per (Family, Level) that
   needs a decision. ThresholdAmount/ActualFamilyAmount/ApproverUserId are
   frozen snapshots at trigger time (same "freeze at decision time"
   convention as ArticlePrice/packaging/classification snapshots elsewhere
   in this codebase) — a later threshold-config change never changes who
   must decide an already-pending step.

   No unique constraint on (OrderId, FamilyId, Level): a rejected order
   reverts to DRAFT and a later re-Submit attempt creates a fresh batch of
   rows (the old ones stay REJECTED/CANCELLED for audit). At most one
   *active* (PENDING/APPROVED) batch can exist per Order at a time because
   Order.Status = PENDING_APPROVAL already blocks a second Submit attempt
   while one is outstanding.

   Idempotent — safe to re-run.
   ============================================================= */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrderApprovalSteps')
BEGIN
    CREATE TABLE OrderApprovalSteps
    (
        OrderApprovalStepId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OrderApprovalStepToken UNIQUEIDENTIFIER   NOT NULL DEFAULT NEWID(),
        OrderId                INT                NOT NULL,
        FamilyId               INT                NOT NULL,
        FamilyCode             VARCHAR(50)        NOT NULL,
        Level                  TINYINT            NOT NULL,
        ThresholdAmount        DECIMAL(18,4)      NOT NULL,
        ActualFamilyAmount     DECIMAL(18,4)      NOT NULL,
        CurrencyCode           VARCHAR(3)         NOT NULL,
        ApproverUserId         INT                NOT NULL,
        Status                 VARCHAR(20)        NOT NULL DEFAULT ('PENDING'),
        DecidedUtc             DATETIME2          NULL,
        DecidedBy              VARCHAR(150)       NULL,
        RejectionReason        NVARCHAR(500)      NULL,
        CreatedUtc             DATETIME2          NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy              VARCHAR(150)       NOT NULL,

        CONSTRAINT FK_OrderApprovalSteps_Order FOREIGN KEY (OrderId) REFERENCES [Order] (OrderId),
        CONSTRAINT FK_OrderApprovalSteps_Families_FamilyId FOREIGN KEY (FamilyId) REFERENCES Families (FamilyId),
        CONSTRAINT FK_OrderApprovalSteps_Users_ApproverUserId FOREIGN KEY (ApproverUserId) REFERENCES Users (UserId),
        CONSTRAINT CK_OrderApprovalSteps_Status CHECK (Status IN (N'PENDING', N'APPROVED', N'REJECTED', N'CANCELLED'))
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderApprovalSteps_OrderId' AND object_id = OBJECT_ID('OrderApprovalSteps'))
BEGIN
    CREATE INDEX IX_OrderApprovalSteps_OrderId ON OrderApprovalSteps (OrderId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderApprovalSteps_ApproverUserId' AND object_id = OBJECT_ID('OrderApprovalSteps'))
BEGIN
    CREATE INDEX IX_OrderApprovalSteps_ApproverUserId ON OrderApprovalSteps (ApproverUserId);
END
GO

PRINT '=== Migration 20260724_OrderApprovalSteps_Create completed successfully ===';
GO
