CREATE OR ALTER PROCEDURE sp_Article_Update
    @ArticleToken     UNIQUEIDENTIFIER,
    @Name             VARCHAR(250),
    @Description      VARCHAR(1000)  = NULL,
    @SupplierSku      VARCHAR(100)   = NULL,
    @Barcode          VARCHAR(100)   = NULL,
    @Brand            VARCHAR(150)   = NULL,
    @FamilyId         INT            = NULL,
    @SubFamilyId      INT            = NULL,
    @PurchaseUnitId   INT,
    @MinimumOrderQty  DECIMAL(18,8)  = NULL,
    @LeadTimeDays     INT            = NULL,
    @TaxCategoryId    INT            = NULL,
    @DefaultReceivingUnitId INT      = NULL,
    @LastUpdatedBy    VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Articles WHERE ArticleToken = @ArticleToken AND IsDeleted = 0)
    BEGIN
        RAISERROR('ARTICLE_NOT_FOUND', 16, 1);
        RETURN;
    END

    UPDATE Articles
    SET    Name             = @Name,
           NormalizedName   = UPPER(@Name),
           Description      = @Description,
           SupplierSku      = @SupplierSku,
           Barcode          = @Barcode,
           Brand            = @Brand,
           FamilyId         = @FamilyId,
           SubFamilyId      = @SubFamilyId,
           PurchaseUnitId   = @PurchaseUnitId,
           MinimumOrderQty  = @MinimumOrderQty,
           LeadTimeDays     = @LeadTimeDays,
           TaxCategoryId    = @TaxCategoryId,
           DefaultReceivingUnitId = @DefaultReceivingUnitId,
           LastUpdatedUtc   = SYSUTCDATETIME(),
           LastUpdatedBy    = @LastUpdatedBy
    WHERE  ArticleToken = @ArticleToken;

    SELECT
        a.ArticleId, a.ArticleToken, a.SupplierId,
        s.Name          AS SupplierName,
        st.Code         AS SupplierType,
        a.Name, a.NormalizedName, a.Description, a.SupplierSku, a.Barcode, a.Brand,
        a.FamilyId,    f.Code  AS FamilyCode, f.NameTranslations AS FamilyNameTranslations,
        a.SubFamilyId, sf.Code AS SubFamilyCode, sf.NameTranslations AS SubFamilyNameTranslations,
        a.PurchaseUnitId, pu.UnitOfMeasureToken AS PurchaseUnitToken, pu.Code AS PurchaseUnitCode,
        pu.Symbol AS PurchaseUnitSymbol, pu.NameTranslations AS PurchaseUnitNameTranslations,
        a.MinimumOrderQty, a.LeadTimeDays,
        a.TaxCategoryId, tc.TaxCategoryToken AS TaxCategoryToken, tc.Code AS TaxCategoryCode,
        COALESCE(a.TaxCategoryId, f.DefaultTaxCategoryId) AS EffectiveTaxCategoryId,
        etc.Code        AS EffectiveTaxCategoryCode,
        a.DefaultReceivingUnitId, dru.UnitOfMeasureToken AS DefaultReceivingUnitToken,
        dru.Code AS DefaultReceivingUnitCode, dru.NameTranslations AS DefaultReceivingUnitNameTranslations,
        a.IsActive, a.IsDeleted,
        a.ReplacedByArticleId, r.ArticleToken AS ReplacedByArticleToken,
        a.DeletedUtc, a.DeletedBy
    FROM   Articles        a
    JOIN   Suppliers       s  ON s.SupplierId       = a.SupplierId
    JOIN   SupplierTypes   st ON st.SupplierTypeId  = s.SupplierTypeId
    JOIN   UnitsOfMeasure  pu ON pu.UnitOfMeasureId = a.PurchaseUnitId
    LEFT JOIN Families     f  ON f.FamilyId         = a.FamilyId
    LEFT JOIN SubFamilies  sf ON sf.SubFamilyId      = a.SubFamilyId
    LEFT JOIN Articles     r  ON r.ArticleId         = a.ReplacedByArticleId
    LEFT JOIN TaxCategories tc  ON tc.TaxCategoryId  = a.TaxCategoryId
    LEFT JOIN TaxCategories etc ON etc.TaxCategoryId = COALESCE(a.TaxCategoryId, f.DefaultTaxCategoryId)
    LEFT JOIN UnitsOfMeasure dru ON dru.UnitOfMeasureId = a.DefaultReceivingUnitId
    WHERE  a.ArticleToken = @ArticleToken;
END;
