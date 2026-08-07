SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ARTICLEDISCOUNT - GET BY SCOPE
   Fetches every active ArticleDiscount sharing the EXACT SAME scope as the
   one being created/edited (same SupplierId, and the same single scope
   dimension — ArticleId, SubFamilyId, FamilyId, or "supplier-wide" when all
   three are NULL) for ArticleDiscountService's C#-side overlap check —
   two discounts at different scope levels are never compared here (an
   Article-level discount overlapping its own Family-level discount is
   allowed by design, since Article always wins on resolution).
   @ExcludeToken skips the row being edited when checking its own new dates.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ArticleDiscount_GetByScope
(
    @SupplierId    INT,
    @ArticleId     INT              = NULL,
    @SubFamilyId   INT              = NULL,
    @FamilyId      INT              = NULL,
    @ExcludeToken  UNIQUEIDENTIFIER = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.ArticleDiscountId, d.ArticleDiscountToken, d.EffectiveFrom, d.EffectiveUntil, d.Description
    FROM dbo.ArticleDiscounts d
    WHERE d.SupplierId = @SupplierId
      AND d.IsActive = 1
      AND (d.ArticleId = @ArticleId OR (d.ArticleId IS NULL AND @ArticleId IS NULL))
      AND (d.SubFamilyId = @SubFamilyId OR (d.SubFamilyId IS NULL AND @SubFamilyId IS NULL))
      AND (d.FamilyId = @FamilyId OR (d.FamilyId IS NULL AND @FamilyId IS NULL))
      AND (@ExcludeToken IS NULL OR d.ArticleDiscountToken <> @ExcludeToken)
    ORDER BY d.EffectiveFrom;
END;
GO
