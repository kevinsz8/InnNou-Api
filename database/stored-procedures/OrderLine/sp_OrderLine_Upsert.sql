SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERLINE - UPSERT
   Adding an Article already present on this Draft Order updates its
   Quantity (and re-snapshots price/unit fields, in case the price
   moved since it was first added) instead of creating a duplicate
   line — mirrors a normal cart's "add to cart" behavior. Structural
   and price snapshot values are resolved by the caller (OrderService,
   via the already-fetched Article + sp_ArticlePrice_GetCurrent), not
   re-derived here.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrderLine_Upsert
(
    @OrderLineToken   UNIQUEIDENTIFIER,
    @OrderId          INT,
    @ArticleId        INT,
    @Quantity         DECIMAL(18,4),
    @PurchaseUnitId   INT,
    @PurchaseQuantity DECIMAL(18,4),
    @ContentUnitId    INT,
    @ContentQuantity  DECIMAL(18,4) = NULL,
    @UnitPrice        DECIMAL(18,4),
    @CurrencyCode     VARCHAR(3),
    @CategoryId       INT           = NULL,
    @CategoryCode     NVARCHAR(50)  = NULL,
    @SubCategoryId    INT           = NULL,
    @SubCategoryCode  NVARCHAR(50)  = NULL,
    @Notes            NVARCHAR(500) = NULL,
    @CreatedBy        VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.OrderLine WHERE OrderId = @OrderId AND ArticleId = @ArticleId)
    BEGIN
        UPDATE dbo.OrderLine
        SET
            Quantity         = @Quantity,
            PurchaseUnitId   = @PurchaseUnitId,
            PurchaseQuantity = @PurchaseQuantity,
            ContentUnitId    = @ContentUnitId,
            ContentQuantity  = @ContentQuantity,
            UnitPrice        = @UnitPrice,
            CurrencyCode     = @CurrencyCode,
            CategoryId       = @CategoryId,
            CategoryCode     = @CategoryCode,
            SubCategoryId    = @SubCategoryId,
            SubCategoryCode  = @SubCategoryCode,
            Notes            = @Notes,
            LastUpdatedUtc   = SYSUTCDATETIME(),
            LastUpdatedBy    = @CreatedBy
        WHERE OrderId = @OrderId AND ArticleId = @ArticleId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.OrderLine
            (OrderLineToken, OrderId, ArticleId, Quantity, PurchaseUnitId, PurchaseQuantity,
             ContentUnitId, ContentQuantity, UnitPrice, CurrencyCode,
             CategoryId, CategoryCode, SubCategoryId, SubCategoryCode, Notes, CreatedBy)
        VALUES
            (@OrderLineToken, @OrderId, @ArticleId, @Quantity, @PurchaseUnitId, @PurchaseQuantity,
             @ContentUnitId, @ContentQuantity, @UnitPrice, @CurrencyCode,
             @CategoryId, @CategoryCode, @SubCategoryId, @SubCategoryCode, @Notes, @CreatedBy);
    END

    SELECT
        ol.OrderLineId, ol.OrderLineToken, ol.OrderId, ord.OrderToken,
        ol.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.SupplierId, s.Name AS SupplierName,
        ol.Quantity,
        ol.PurchaseUnitId, pu.Code AS PurchaseUnitCode,
        ol.PurchaseQuantity,
        ol.ContentUnitId, cu.Code AS ContentUnitCode,
        ol.ContentQuantity,
        ol.UnitPrice, ol.CurrencyCode,
        ol.CategoryId, ol.CategoryCode, ol.SubCategoryId, ol.SubCategoryCode,
        ol.Notes,
        ol.CreatedUtc, ol.CreatedBy, ol.LastUpdatedUtc, ol.LastUpdatedBy
    FROM dbo.OrderLine ol
    JOIN dbo.[Order] ord       ON ord.OrderId        = ol.OrderId
    JOIN dbo.Articles a        ON a.ArticleId        = ol.ArticleId
    JOIN dbo.Suppliers s       ON s.SupplierId       = a.SupplierId
    JOIN dbo.UnitsOfMeasure pu ON pu.UnitOfMeasureId = ol.PurchaseUnitId
    JOIN dbo.UnitsOfMeasure cu ON cu.UnitOfMeasureId = ol.ContentUnitId
    WHERE ol.OrderId = @OrderId AND ol.ArticleId = @ArticleId;
END;
GO
