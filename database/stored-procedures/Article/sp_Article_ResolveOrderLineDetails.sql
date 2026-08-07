CREATE OR ALTER PROCEDURE sp_Article_ResolveOrderLineDetails
    @ArticleId      INT,
    @SupplierId     INT,
    @OrganizationId INT,
    @WarehouseId    INT,
    @AsOfDate       DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Combines OrderService.AddLineAsync's 5 independent post-article-resolution lookups
    -- (packaging, zone coverage, price, discount, classification — all keyed off the ArticleId/
    -- SupplierId/OrganizationId/WarehouseId already resolved by the caller's own
    -- sp_Article_GetByToken call) into ONE round trip via multiple result sets, read through
    -- Dapper's QueryMultipleAsync. Article resolution itself deliberately stays a separate,
    -- unchanged call in AddLineAsync rather than being folded in here — sp_Article_GetByToken's
    -- full projection (~46 columns incl. hierarchy-based visibility/Favorites/Classification) has
    -- no stable contract to mirror via INSERT...EXEC without real schema-drift risk on every
    -- future column added there. Each EXEC below forwards that sub-procedure's own SELECT as-is,
    -- so none of their business logic (delivery-zone resolution, discount priority, classification
    -- ascending-hierarchy walk) is duplicated here — this stays a "dumb" fan-out. See
    -- 2026-08-07's Performance Optimization backlog item #1.

    -- Result set 1: packaging levels
    EXEC dbo.sp_ArticlePackagingLevel_GetByArticleId @ArticleId = @ArticleId;

    -- Result set 2: zone coverage
    EXEC dbo.sp_SupplierDeliveryZone_CheckCoverage @SupplierId = @SupplierId, @WarehouseId = @WarehouseId;

    -- Result set 3: price row (0 or 1 rows)
    DECLARE @CurrencyOut VARCHAR(10) = NULL;
    EXEC dbo.sp_ArticlePrice_GetCurrent @ArticleId = @ArticleId, @OrganizationId = @OrganizationId, @CurrencyCode = @CurrencyOut OUTPUT, @AsOfDate = @AsOfDate;

    -- Result set 4: resolved currency, always exactly 1 row — an OUTPUT param can't be read
    -- directly off a GridReader result set, so this surfaces it as one.
    SELECT @CurrencyOut AS ResolvedCurrencyCode;

    -- Result set 5: effective discount (0 or 1 rows)
    EXEC dbo.sp_ArticleDiscount_GetEffective @ArticleId = @ArticleId, @AsOfDate = @AsOfDate;

    -- Result set 6: effective classification (0 or 1 rows)
    EXEC dbo.sp_ArticleClassification_GetEffectiveForArticle @ArticleId = @ArticleId, @OrganizationId = @OrganizationId;
END;
