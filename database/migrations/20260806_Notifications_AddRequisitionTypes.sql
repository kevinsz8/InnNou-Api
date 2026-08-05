-- =============================================================
-- MIGRATION: Seed 4 additional NotificationTypes for Requisiciones Internas
-- (single resolvable owner — the Requisition's own CreatedBy — same auto-fire
-- pattern as the original "Bucket 1" types, no subscription infrastructure needed)
-- Date: 2026-08-06
-- =============================================================
-- Idempotent — safe to re-run.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Seed order matters — the C# NotificationType enum hardcodes these Ids (15-18).
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'REQUISITION_APPROVED')
    INSERT INTO NotificationTypes (Code) VALUES ('REQUISITION_APPROVED');
GO
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'REQUISITION_REJECTED')
    INSERT INTO NotificationTypes (Code) VALUES ('REQUISITION_REJECTED');
GO
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'REQUISITION_ISSUED')
    INSERT INTO NotificationTypes (Code) VALUES ('REQUISITION_ISSUED');
GO
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'REQUISITION_CLOSED_SHORT')
    INSERT INTO NotificationTypes (Code) VALUES ('REQUISITION_CLOSED_SHORT');
GO

PRINT '=== Migration 20260806_Notifications_AddRequisitionTypes completed successfully ===';
GO
