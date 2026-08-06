-- =============================================================
-- MIGRATION: Seed the 5th Requisicion notification type -- fan-out to every
-- possible approver (RoleLevel >= 20 within the Requisition's own
-- Organization, warehouse-scope filtered), not a single resolvable owner like
-- the original 4 (Approved/Rejected/Issued/ClosedShort). See
-- sp_Requisition_GetPossibleApprovers.
-- Date: 2026-08-06
-- =============================================================
-- Idempotent -- safe to re-run.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Id matters -- the C# NotificationType enum hardcodes this as 19.
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'REQUISITION_REQUESTED')
    INSERT INTO NotificationTypes (Code) VALUES ('REQUISITION_REQUESTED');
GO

PRINT '=== Migration 20260806_Notifications_AddRequisitionRequestedType completed successfully ===';
GO
