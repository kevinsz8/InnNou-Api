-- =============================================================
-- MIGRATION: Seed the Article_Discount_Created notification type -- fired
-- when a Supplier configures a new discount (see
-- ArticleDiscountService.CreateAsync), fanned out to every user with an
-- active SupplierPriceChangeSubscription for that Supplier (same
-- subscriber list ArticlePriceService.NotifySupplierPriceSubscribersAsync
-- already uses for Supplier_Price_Updated).
-- Date: 2026-08-07
-- =============================================================
-- Idempotent -- safe to re-run.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Id matters -- the C# NotificationType enum hardcodes this as 20.
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'ARTICLE_DISCOUNT_CREATED')
    INSERT INTO NotificationTypes (Code) VALUES ('ARTICLE_DISCOUNT_CREATED');
GO

PRINT '=== Migration 20260807_Notifications_AddArticleDiscountCreatedType completed successfully ===';
GO
