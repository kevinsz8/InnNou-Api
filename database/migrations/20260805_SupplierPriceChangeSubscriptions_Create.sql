-- =============================================================
-- MIGRATION: Create SupplierPriceChangeSubscriptions + seed SUPPLIER_PRICE_UPDATED NotificationType
-- Date: 2026-08-05
-- =============================================================
-- Opt-in "watch this supplier" subscription — first real use case of the subscription-layer
-- pattern discussed alongside the Notifications module (see .claude/NotificationsModule.md).
-- Deliberately per-specific-supplier (like watching a single GitHub repo), never a blanket
-- "notify me about every supplier" toggle — keeps volume bounded and matches what the user
-- actually cares about. Only global list-price changes (ArticlePrices.OrganizationId IS NULL)
-- trigger this; negotiated per-organization contract prices are usually entered by internal
-- staff, not the supplier, so they're out of scope for "the supplier updated a price".
--
-- Same shape as ArticleFavorites: a toggle join-table, not audited history — no
-- IsActive/IsDeleted/LastUpdated*, unsubscribing is a physical DELETE.
--
-- Idempotent — safe to re-run.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('SupplierPriceChangeSubscriptions', 'U') IS NULL
BEGIN
    CREATE TABLE SupplierPriceChangeSubscriptions
    (
        SupplierPriceChangeSubscriptionId    INT              IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierPriceChangeSubscriptionToken UNIQUEIDENTIFIER NOT NULL UNIQUE DEFAULT NEWID(),
        UserId                                INT              NOT NULL,
        SupplierId                            INT              NOT NULL,
        CreatedUtc                            DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                             VARCHAR(150)     NOT NULL,

        CONSTRAINT FK_SupplierPriceChangeSubscriptions_User     FOREIGN KEY (UserId)     REFERENCES Users (UserId),
        CONSTRAINT FK_SupplierPriceChangeSubscriptions_Supplier FOREIGN KEY (SupplierId) REFERENCES Suppliers (SupplierId)
    );
END
GO

-- Makes "subscribe twice" race-safe and is what sp_SupplierPriceChangeSubscription_Set's
-- reconciliation MERGE keys off.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_SupplierPriceChangeSubscriptions_User_Supplier')
BEGIN
    CREATE UNIQUE INDEX UX_SupplierPriceChangeSubscriptions_User_Supplier
        ON SupplierPriceChangeSubscriptions (UserId, SupplierId);
END
GO

-- Hot lookup at price-change time: "who is subscribed to this Supplier".
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierPriceChangeSubscriptions_SupplierId')
BEGIN
    CREATE INDEX IX_SupplierPriceChangeSubscriptions_SupplierId ON SupplierPriceChangeSubscriptions (SupplierId);
END
GO

-- ── New NotificationType — seed order matters, the C# NotificationType enum hardcodes Ids ──
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'SUPPLIER_PRICE_UPDATED')
    INSERT INTO NotificationTypes (Code) VALUES ('SUPPLIER_PRICE_UPDATED');
GO

PRINT '=== Migration 20260805_SupplierPriceChangeSubscriptions_Create completed successfully ===';
GO
