CREATE OR ALTER PROCEDURE sp_Article_Create
    @ArticleToken     UNIQUEIDENTIFIER,
    @SupplierId       INT,
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
    @CreatedBy        VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Articles
        (ArticleToken, SupplierId, Name, NormalizedName, Description,
         SupplierSku, Barcode, Brand,
         FamilyId, SubFamilyId,
         PurchaseUnitId,
         MinimumOrderQty, LeadTimeDays,
         CreatedBy)
    VALUES
        (@ArticleToken, @SupplierId, @Name, UPPER(@Name), @Description,
         @SupplierSku, @Barcode, @Brand,
         @FamilyId, @SubFamilyId,
         @PurchaseUnitId,
         @MinimumOrderQty, @LeadTimeDays,
         @CreatedBy);

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
