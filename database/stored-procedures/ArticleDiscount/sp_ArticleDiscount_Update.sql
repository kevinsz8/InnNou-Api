SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- SupplierId/ArticleId/SubFamilyId/FamilyId (the row's scope/identity) are immutable after
-- create, same convention as FamilyApprovalThreshold's OrganizationId/FamilyId/Level — changing
-- scope means a different discount, not an edit of this one. Only the terms are editable.
CREATE OR ALTER PROCEDURE sp_ArticleDiscount_Update
    @ArticleDiscountToken UNIQUEIDENTIFIER,
    @DiscountTypeId       INT,
    @DiscountValue        DECIMAL(18,8),
    @CurrencyCode         VARCHAR(3)    = NULL,
    @EffectiveFrom        DATE,
    @EffectiveUntil       DATE          = NULL,
    @Description          NVARCHAR(300) = NULL,
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
    SET    DiscountTypeId  = @DiscountTypeId,
           DiscountValue   = @DiscountValue,
           CurrencyCode    = @CurrencyCode,
           EffectiveFrom   = @EffectiveFrom,
           EffectiveUntil  = @EffectiveUntil,
           Description     = @Description,
           LastUpdatedUtc  = SYSUTCDATETIME(),
           LastUpdatedBy   = @LastUpdatedBy
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
