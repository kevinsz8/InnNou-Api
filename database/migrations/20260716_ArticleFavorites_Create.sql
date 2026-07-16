-- =============================================================
-- MIGRATION: Create ArticleFavorites table
-- Date: 2026-07-16
-- =============================================================
-- Per-organization "favorite article" shortlist. OrganizationId is
-- always the marking organization (never NULL/global) — a favorite
-- marked by an organization automatically cascades to all of its
-- descendants when resolved via sp_ArticleFavorite_GetEffective's
-- ancestor walk (same recursive-CTE shape as
-- sp_Organization_ResolveCurrencyCode, just upward through
-- ParentOrganizationId). Strictly additive: a descendant can add its
-- own extra favorites, but can never remove/override a row owned by
-- an ancestor — sp_ArticleFavorite_Delete only ever targets the
-- caller's own (ArticleId, OrganizationId) row.
-- No IsActive/IsDeleted/LastUpdated* — this is a toggle join-table,
-- not audited history. Unmarking is a physical DELETE.
-- Guarded so it is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('ArticleFavorites', 'U') IS NULL
BEGIN
    CREATE TABLE ArticleFavorites
    (
        ArticleFavoriteId    INT              IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ArticleFavoriteToken UNIQUEIDENTIFIER NOT NULL UNIQUE DEFAULT NEWID(),
        ArticleId            INT              NOT NULL,
        OrganizationId       INT              NOT NULL,
        CreatedUtc           DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy            VARCHAR(150)     NOT NULL,

        CONSTRAINT FK_ArticleFavorites_Article      FOREIGN KEY (ArticleId)      REFERENCES Articles (ArticleId),
        CONSTRAINT FK_ArticleFavorites_Organization FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId)
    );
END
GO

-- Makes "mark twice" race-safe and is what sp_ArticleFavorite_GetEffective's ancestor-CTE
-- join keys off.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ArticleFavorites_Article_Organization')
BEGIN
    CREATE UNIQUE INDEX UX_ArticleFavorites_Article_Organization
        ON ArticleFavorites (ArticleId, OrganizationId);
END
GO

-- FK lookups/joins — SQL Server does not auto-index foreign keys.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ArticleFavorites_OrganizationId')
BEGIN
    CREATE INDEX IX_ArticleFavorites_OrganizationId ON ArticleFavorites (OrganizationId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ArticleFavorites_ArticleId')
BEGIN
    CREATE INDEX IX_ArticleFavorites_ArticleId ON ArticleFavorites (ArticleId);
END
GO
