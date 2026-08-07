SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ARTICLEDISCOUNT - GET EFFECTIVE
   Single-row resolution of "what discount (if any) applies to this Article
   as of @AsOfDate", priority Article-specific > SubFamily > Family >
   Supplier-wide (all NULL scope) — same "most specific wins, no stacking"
   shape as sp_ParLevel_GetEffective's EVENT > SEASONAL > BASE, and the same
   deliberate, narrow exception to "SPs stay dumb": this must be reused
   identically wherever a discount is resolved (today: OrderService.AddLineAsync),
   is genuinely SQL-native (date-window + scope-priority resolution), and doing
   it in C# would mean either N+1 lookups or risking the priority logic
   silently diverging between hand-written copies.

   Returns zero rows when no discount matches (the normal case) — the caller
   should not throw, just skip applying a discount.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ArticleDiscount_GetEffective
(
    @ArticleId INT,
    @AsOfDate  DATE
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SupplierId   INT;
    DECLARE @FamilyId     INT;
    DECLARE @SubFamilyId  INT;

    SELECT @SupplierId = SupplierId, @FamilyId = FamilyId, @SubFamilyId = SubFamilyId
    FROM dbo.Articles WHERE ArticleId = @ArticleId;

    IF @SupplierId IS NULL
        RETURN;

    SELECT TOP 1
        d.ArticleDiscountId, d.ArticleDiscountToken, d.SupplierId, s.SupplierToken,
        d.ArticleId, d.SubFamilyId, d.FamilyId,
        d.DiscountTypeId, dt.Code AS DiscountTypeCode, d.DiscountValue, d.CurrencyCode,
        d.EffectiveFrom, d.EffectiveUntil, d.Description,
        CASE WHEN d.ArticleId IS NOT NULL THEN 'ARTICLE'
             WHEN d.SubFamilyId IS NOT NULL THEN 'SUBFAMILY'
             WHEN d.FamilyId IS NOT NULL THEN 'FAMILY'
             ELSE 'SUPPLIER' END AS ScopeLevel
    FROM dbo.ArticleDiscounts d
    JOIN dbo.DiscountTypes dt ON dt.DiscountTypeId = d.DiscountTypeId
    JOIN dbo.Suppliers s ON s.SupplierId = d.SupplierId
    WHERE d.SupplierId = @SupplierId
      AND d.IsActive = 1
      AND d.EffectiveFrom <= @AsOfDate
      AND (d.EffectiveUntil IS NULL OR d.EffectiveUntil >= @AsOfDate)
      AND (
            d.ArticleId = @ArticleId
         OR (d.ArticleId IS NULL AND d.SubFamilyId IS NOT NULL AND d.SubFamilyId = @SubFamilyId)
         OR (d.ArticleId IS NULL AND d.SubFamilyId IS NULL AND d.FamilyId IS NOT NULL AND d.FamilyId = @FamilyId)
         OR (d.ArticleId IS NULL AND d.SubFamilyId IS NULL AND d.FamilyId IS NULL)
          )
    ORDER BY
        CASE WHEN d.ArticleId IS NOT NULL THEN 0
             WHEN d.SubFamilyId IS NOT NULL THEN 1
             WHEN d.FamilyId IS NOT NULL THEN 2
             ELSE 3 END;
END;
GO
