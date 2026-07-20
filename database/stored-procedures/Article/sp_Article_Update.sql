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
    @MinimumOrderQty  DECIMAL(18,4)  = NULL,
    @LeadTimeDays     INT            = NULL,
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
           LastUpdatedUtc   = SYSUTCDATETIME(),
           LastUpdatedBy    = @LastUpdatedBy
    WHERE  ArticleToken = @ArticleToken;

    SELECT
        a.ArticleId, a.ArticleToken, a.SupplierId,
        s.Name          AS SupplierName,
        a.Name, a.NormalizedName, a.Description, a.SupplierSku, a.Barcode, a.Brand,
        a.FamilyId,    f.Code  AS FamilyCode,
        a.SubFamilyId, sf.Code AS SubFamilyCode,
        a.PurchaseUnitId, pu.Code AS PurchaseUnitCode, pu.Symbol AS PurchaseUnitSymbol,
        a.MinimumOrderQty, a.LeadTimeDays,
        a.IsActive, a.IsDeleted,
        a.ReplacedByArticleId, r.ArticleToken AS ReplacedByArticleToken
    FROM   Articles        a
    JOIN   Suppliers       s  ON s.SupplierId       = a.SupplierId
    JOIN   UnitsOfMeasure  pu ON pu.UnitOfMeasureId = a.PurchaseUnitId
    LEFT JOIN Families     f  ON f.FamilyId         = a.FamilyId
    LEFT JOIN SubFamilies  sf ON sf.SubFamilyId      = a.SubFamilyId
    LEFT JOIN Articles     r  ON r.ArticleId         = a.ReplacedByArticleId
    WHERE  a.ArticleToken = @ArticleToken;
END;
