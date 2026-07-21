SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: FamilyApprovalThresholds (per-Asociado spend-approval config)
   Each Asociado (ASSOCIATE-typed organization) can configure, per Family,
   one or more sequential approval Levels — e.g. Level 1 requires approval
   past $1000, Level 2 past $5000. Crossing Level 2 requires BOTH Level 1's
   and Level 2's designated approver to sign off, in order (see
   OrderApprovalSteps, added in the same migration set, for the
   per-Order execution/audit trail).

   ApproverUserId is a fixed, designated user per (Organization, Family,
   Level) — not a role-level computation. App-enforced: must be within the
   organization's own hierarchy (that Asociado or its Super Asociado),
   checked via the existing sp_Organization_IsInHierarchy.

   Level must be contiguous starting at 1 per (OrganizationId, FamilyId),
   and ThresholdAmount strictly increasing with Level — both validated
   app-side (FamilyApprovalThresholdService), same convention as
   ArticlePackagingLevel's SequenceOrder validation.

   Idempotent — safe to re-run.
   ============================================================= */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FamilyApprovalThresholds')
BEGIN
    CREATE TABLE FamilyApprovalThresholds
    (
        FamilyApprovalThresholdId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FamilyApprovalThresholdToken UNIQUEIDENTIFIER   NOT NULL DEFAULT NEWID(),
        OrganizationId               INT                NOT NULL,
        FamilyId                     INT                NOT NULL,
        Level                        TINYINT            NOT NULL,
        ThresholdAmount              DECIMAL(18,4)      NOT NULL,
        ApproverUserId               INT                NOT NULL,
        IsActive                     BIT                NOT NULL DEFAULT (1),
        CreatedUtc                   DATETIME2          NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                    VARCHAR(150)       NULL,
        LastUpdatedUtc               DATETIME2          NULL,
        LastUpdatedBy                VARCHAR(150)       NULL,

        CONSTRAINT FK_FamilyApprovalThresholds_Organizations_OrganizationId
            FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId),
        CONSTRAINT FK_FamilyApprovalThresholds_Families_FamilyId
            FOREIGN KEY (FamilyId) REFERENCES Families (FamilyId),
        CONSTRAINT FK_FamilyApprovalThresholds_Users_ApproverUserId
            FOREIGN KEY (ApproverUserId) REFERENCES Users (UserId),
        CONSTRAINT CK_FamilyApprovalThresholds_Level CHECK (Level >= 1),
        CONSTRAINT CK_FamilyApprovalThresholds_ThresholdAmount CHECK (ThresholdAmount > 0)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_FamilyApprovalThresholds_Org_Family_Level' AND object_id = OBJECT_ID('FamilyApprovalThresholds'))
BEGIN
    CREATE UNIQUE INDEX UX_FamilyApprovalThresholds_Org_Family_Level ON FamilyApprovalThresholds (OrganizationId, FamilyId, Level);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FamilyApprovalThresholds_FamilyId' AND object_id = OBJECT_ID('FamilyApprovalThresholds'))
BEGIN
    CREATE INDEX IX_FamilyApprovalThresholds_FamilyId ON FamilyApprovalThresholds (FamilyId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FamilyApprovalThresholds_ApproverUserId' AND object_id = OBJECT_ID('FamilyApprovalThresholds'))
BEGIN
    CREATE INDEX IX_FamilyApprovalThresholds_ApproverUserId ON FamilyApprovalThresholds (ApproverUserId);
END
GO

PRINT '=== Migration 20260724_FamilyApprovalThresholds_Create completed successfully ===';
GO
