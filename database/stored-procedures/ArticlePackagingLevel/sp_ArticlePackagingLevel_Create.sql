SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ARTICLE PACKAGING LEVEL - CREATE
   Called once per level, inside the same transaction as
   sp_Article_Create/sp_Article_Create (via Supersede) — never
   updated in place, only ever created together with a new Article
   row (structural fields, including the packaging chain, are
   immutable — see Article structural versioning in CLAUDE.md).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ArticlePackagingLevel_Create
(
    @ArticlePackagingLevelToken UNIQUEIDENTIFIER,
    @ArticleId                  INT,
    @SequenceOrder               TINYINT,
    @UnitOfMeasureId              INT,
    @QuantityInParentUnit          DECIMAL(18,4),
    @IsDefinedUnit                 BIT,
    @CreatedBy                     VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ArticlePackagingLevels
        (ArticlePackagingLevelToken, ArticleId, SequenceOrder, UnitOfMeasureId, QuantityInParentUnit, IsDefinedUnit, CreatedBy)
    VALUES
        (@ArticlePackagingLevelToken, @ArticleId, @SequenceOrder, @UnitOfMeasureId, @QuantityInParentUnit, @IsDefinedUnit, @CreatedBy);
END;
GO
