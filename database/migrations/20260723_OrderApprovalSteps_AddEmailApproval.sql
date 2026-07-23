-- Adds anonymous single-use email-approval support to OrderApprovalSteps. A SEPARATE token
-- from OrderApprovalStepToken (which is already used by the authenticated flow) — deliberately
-- never reused for this publicly-emailed, anonymous surface. Idempotent/rerunnable.

IF COL_LENGTH('dbo.OrderApprovalSteps', 'EmailApprovalToken') IS NULL
    ALTER TABLE OrderApprovalSteps ADD EmailApprovalToken UNIQUEIDENTIFIER NULL;
GO

IF COL_LENGTH('dbo.OrderApprovalSteps', 'EmailApprovalTokenExpiresUtc') IS NULL
    ALTER TABLE OrderApprovalSteps ADD EmailApprovalTokenExpiresUtc DATETIME2(7) NULL;
GO

IF COL_LENGTH('dbo.OrderApprovalSteps', 'EmailApprovalTokenUsedUtc') IS NULL
    ALTER TABLE OrderApprovalSteps ADD EmailApprovalTokenUsedUtc DATETIME2(7) NULL;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_OrderApprovalSteps_EmailApprovalToken')
    CREATE UNIQUE INDEX UX_OrderApprovalSteps_EmailApprovalToken
        ON OrderApprovalSteps(EmailApprovalToken)
        WHERE EmailApprovalToken IS NOT NULL;
GO
