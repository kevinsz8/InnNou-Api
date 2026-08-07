SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE sp_ArticleDiscount_SetActive
    @ArticleDiscountToken UNIQUEIDENTIFIER,
    @IsActive             BIT,
    @LastUpdatedBy        VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM ArticleDiscounts WHERE ArticleDiscountToken = @ArticleDiscountToken)
    BEGIN
        RAISERROR('ARTICLE_DISCOUNT_NOT_FOUND', 16, 1);
        RETURN;
    END

    UPDATE ArticleDiscounts
    SET    IsActive       = @IsActive,
           LastUpdatedUtc = SYSUTCDATETIME(),
           LastUpdatedBy  = @LastUpdatedBy
    WHERE  ArticleDiscountToken = @ArticleDiscountToken;

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
