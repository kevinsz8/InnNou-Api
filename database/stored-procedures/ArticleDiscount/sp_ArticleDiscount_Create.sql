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

    -- ArticleDiscountService.EnsureNoOverlapAsync already ran this same check in C# before this
    -- call — this is a DB-level backstop for the race window between that check and this INSERT
    -- (two concurrent Creates for the identical scope+overlapping dates), same "C# primary check,
    -- DB backstop" shape as sp_StockLevel_ApplyDelta. Unlike StockLevel's single-row UPDATE (which
    -- serializes for free via row locking), this is an INSERT-vs-absence-of-conflict check, which
    -- needs an explicit app lock to actually block a concurrent writer under READ COMMITTED.
    DECLARE @LockResource NVARCHAR(200) =
        'ArticleDiscountScope:' + CAST(@SupplierId AS NVARCHAR(20)) + ':' +
        ISNULL('A' + CAST(@ArticleId AS NVARCHAR(20)),
        ISNULL('SF' + CAST(@SubFamilyId AS NVARCHAR(20)),
        ISNULL('F' + CAST(@FamilyId AS NVARCHAR(20)), 'ALL')));
    DECLARE @LockResult INT;

    BEGIN TRANSACTION;

    EXEC @LockResult = sp_getapplock @Resource = @LockResource, @LockMode = 'Exclusive',
        @LockOwner = 'Transaction', @LockTimeout = 10000;
    IF @LockResult < 0
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('ARTICLE_DISCOUNT_LOCK_TIMEOUT', 16, 1);
        RETURN;
    END

    IF EXISTS (
        SELECT 1 FROM ArticleDiscounts d
        WHERE d.SupplierId = @SupplierId
          AND d.IsActive = 1
          AND (d.ArticleId = @ArticleId OR (d.ArticleId IS NULL AND @ArticleId IS NULL))
          AND (d.SubFamilyId = @SubFamilyId OR (d.SubFamilyId IS NULL AND @SubFamilyId IS NULL))
          AND (d.FamilyId = @FamilyId OR (d.FamilyId IS NULL AND @FamilyId IS NULL))
          AND d.EffectiveFrom <= ISNULL(@EffectiveUntil, '9999-12-31')
          AND ISNULL(d.EffectiveUntil, '9999-12-31') >= @EffectiveFrom
    )
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('ARTICLE_DISCOUNT_OVERLAPPING', 16, 1);
        RETURN;
    END

    INSERT INTO ArticleDiscounts
        (ArticleDiscountToken, SupplierId, ArticleId, SubFamilyId, FamilyId,
         DiscountTypeId, DiscountValue, CurrencyCode, EffectiveFrom, EffectiveUntil, Description, CreatedBy)
    VALUES
        (@ArticleDiscountToken, @SupplierId, @ArticleId, @SubFamilyId, @FamilyId,
         @DiscountTypeId, @DiscountValue, @CurrencyCode, @EffectiveFrom, @EffectiveUntil, @Description, @CreatedBy);

    COMMIT TRANSACTION;

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
