-- =============================================================
-- MIGRATION: Create ArticlePackagingLevels
-- Date: 2026-07-22
-- =============================================================
-- Replaces Articles' old fixed 2-level packaging shape
-- (PurchaseUnit/PurchaseQuantity + ContentUnit/ContentQuantity +
-- optional BaseUnit) with an N-level chain, one row per level,
-- ordered by SequenceOrder (1 = closest to the purchase unit).
--
-- Every level except the terminal one is an "Unidad Indefinida" —
-- a COUNT container (Caja, Botella, Pallet, Bidon) that only says
-- "how many of the next level are inside", never a fixed physical
-- quantity on its own. Exactly one level per Article is the
-- "Unidad Definida" (IsDefinedUnit = 1) — the level that actually
-- closes the chain with a real, fixed quantity (Litro, Kilogramo,
-- or an atomic COUNT like "Unidad"/"Guante" when there is no
-- further physical breakdown). It must always be the last
-- (highest SequenceOrder) row for that article.
--
-- This is deliberately a per-article, positional role (IsDefinedUnit
-- lives on the row, not on UnitsOfMeasure/UnitTypes) rather than a
-- flag fixed globally in the unit catalog — matches the dominant
-- industry pattern (SAP Material Master's per-material Base Unit of
-- Measure, GS1's item-relative packaging hierarchy), confirmed via
-- research before this was built. See InnNou-Api CLAUDE.md,
-- "Article packaging levels", for the full design rationale.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('ArticlePackagingLevels', 'U') IS NULL
BEGIN
    CREATE TABLE ArticlePackagingLevels
    (
        ArticlePackagingLevelId    int              NOT NULL IDENTITY(1,1),
        ArticlePackagingLevelToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        ArticleId                  int              NOT NULL,

        SequenceOrder              tinyint          NOT NULL,
        UnitOfMeasureId            int              NOT NULL,
        QuantityInParentUnit       decimal(18,4)    NOT NULL,
        IsDefinedUnit              bit              NOT NULL DEFAULT (0),

        CreatedUtc                 datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                  varchar(150)         NULL,

        CONSTRAINT PK_ArticlePackagingLevels PRIMARY KEY (ArticlePackagingLevelId),
        CONSTRAINT FK_ArticlePackagingLevels_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId),
        CONSTRAINT FK_ArticlePackagingLevels_UnitsOfMeasure FOREIGN KEY (UnitOfMeasureId) REFERENCES UnitsOfMeasure (UnitOfMeasureId),
        CONSTRAINT CK_ArticlePackagingLevels_SequenceOrder CHECK (SequenceOrder >= 1),
        CONSTRAINT CK_ArticlePackagingLevels_QuantityPositive CHECK (QuantityInParentUnit > 0)
    );

    CREATE UNIQUE INDEX UQ_ArticlePackagingLevels_Token ON ArticlePackagingLevels (ArticlePackagingLevelToken);
    CREATE        INDEX IX_ArticlePackagingLevels_ArticleId ON ArticlePackagingLevels (ArticleId);

    -- Exactly one level per article, and no two levels sharing a position.
    CREATE UNIQUE INDEX UX_ArticlePackagingLevels_Article_Sequence
        ON ArticlePackagingLevels (ArticleId, SequenceOrder);

    CREATE UNIQUE INDEX UX_ArticlePackagingLevels_Article_DefinedUnit
        ON ArticlePackagingLevels (ArticleId) WHERE IsDefinedUnit = 1;
END
GO
