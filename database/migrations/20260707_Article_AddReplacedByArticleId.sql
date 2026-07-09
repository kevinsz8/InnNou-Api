-- =============================================================
-- MIGRATION: Add Articles.ReplacedByArticleId
-- Date: 2026-07-07
-- =============================================================
-- Supports the Article "supersede" flow: structural changes
-- (purchase/content unit or quantities) create a new Article row
-- instead of mutating the existing one in place, so historical
-- PO/GR/Inventory references keep their original meaning. The
-- old row is deactivated and points forward to its replacement.
-- Guarded so it is a no-op if already applied.
-- =============================================================

IF COL_LENGTH('Articles', 'ReplacedByArticleId') IS NULL
BEGIN
    ALTER TABLE Articles ADD ReplacedByArticleId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Articles_ReplacedByArticle')
BEGIN
    ALTER TABLE Articles
        ADD CONSTRAINT FK_Articles_ReplacedByArticle
        FOREIGN KEY (ReplacedByArticleId) REFERENCES Articles (ArticleId);
END
GO
