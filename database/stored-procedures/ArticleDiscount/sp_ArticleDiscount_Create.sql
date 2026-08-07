SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- ArticleDiscounts has filtered indexes (IX_ArticleDiscounts_ArticleId/SubFamilyId/FamilyId/
-- SupplierWide) — INSERT against a table with a filtered index requires QUOTED_IDENTIFIER ON at
-- the session that created this procedure, not just at index creation time (error 1934 otherwise).
CREATE OR ALTER PROCEDURE sp_ArticleDiscount_Create
    @ArticleDiscountToken UNIQUEIDENTIFIER,
    @SupplierId           INT,
    @ArticleId            INT           = NULL,
    @SubFamilyId          INT           = NULL,
    @FamilyId             INT           = NULL,
    @DiscountTypeId       INT,
    @DiscountValue        DECIMAL(18,8),
    @CurrencyCode         VARCHAR(3)    = NULL,
    @EffectiveFrom        DATE,
    @EffectiveUntil       DATE          = NULL,
    @Description          NVARCHAR(300) = NULL,
    @CreatedBy            VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ArticleDiscounts
        (ArticleDiscountToken, SupplierId, ArticleId, SubFamilyId, FamilyId,
         DiscountTypeId, DiscountValue, CurrencyCode, EffectiveFrom, EffectiveUntil, Description, CreatedBy)
    VALUES
        (@ArticleDiscountToken, @SupplierId, @ArticleId, @SubFamilyId, @FamilyId,
         @DiscountTypeId, @DiscountValue, @CurrencyCode, @EffectiveFrom, @EffectiveUntil, @Description, @CreatedBy);

    SELECT
        d.ArticleDiscountId, d.ArticleDiscountToken,
        d.SupplierId, s.SupplierToken, s.Name AS SupplierName,
        d.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        d.SubFamilyId, sf.SubFamilyToken, sf.Code AS SubFamilyCode,
        d.FamilyId, f.FamilyToken, f.Code AS FamilyCode,
        d.DiscountTypeId, dt.Code AS DiscountTypeCode, d.DiscountValue, d.CurrencyCode,
        d.EffectiveFrom, d.EffectiveUntil, d.Description,
        d.IsActive, d.CreatedUtc, d.CreatedBy, d.LastUpdatedUtc, d.LastUpdatedBy
    FROM ArticleDiscounts d
    JOIN Suppliers s              ON s.SupplierId    = d.SupplierId
    JOIN DiscountTypes dt         ON dt.DiscountTypeId = d.DiscountTypeId
    LEFT JOIN Articles a          ON a.ArticleId     = d.ArticleId
    LEFT JOIN SubFamilies sf      ON sf.SubFamilyId  = d.SubFamilyId
    LEFT JOIN Families f          ON f.FamilyId      = d.FamilyId
    WHERE d.ArticleDiscountToken = @ArticleDiscountToken;
END;
GO
