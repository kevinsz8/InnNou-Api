-- =============================================================
-- MIGRATION: Seed 8 additional NotificationTypes ("Bucket 1" — single resolvable owner,
-- same auto-fire pattern as the original 6 types, no subscription infrastructure needed)
-- Date: 2026-08-05
-- =============================================================
-- Idempotent — safe to re-run.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Seed order matters — the C# NotificationType enum hardcodes these Ids (7-14).
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'GOODS_RECEIPT_CREATED')
    INSERT INTO NotificationTypes (Code) VALUES ('GOODS_RECEIPT_CREATED');
GO
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'PURCHASE_ORDER_RECTIFIED')
    INSERT INTO NotificationTypes (Code) VALUES ('PURCHASE_ORDER_RECTIFIED');
GO
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'INTERNAL_ORDER_SHIPPED')
    INSERT INTO NotificationTypes (Code) VALUES ('INTERNAL_ORDER_SHIPPED');
GO
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'INTERNAL_ORDER_RECEIVED')
    INSERT INTO NotificationTypes (Code) VALUES ('INTERNAL_ORDER_RECEIVED');
GO
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'INTERNAL_ORDER_CANCELLED')
    INSERT INTO NotificationTypes (Code) VALUES ('INTERNAL_ORDER_CANCELLED');
GO
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'SUPPLIER_RETURN_CLOSED')
    INSERT INTO NotificationTypes (Code) VALUES ('SUPPLIER_RETURN_CLOSED');
GO
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'IMPERSONATION_STARTED')
    INSERT INTO NotificationTypes (Code) VALUES ('IMPERSONATION_STARTED');
GO
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'USER_ROLE_CHANGED')
    INSERT INTO NotificationTypes (Code) VALUES ('USER_ROLE_CHANGED');
GO

PRINT '=== Migration 20260805_Notifications_AddBucket1Types completed successfully ===';
GO
