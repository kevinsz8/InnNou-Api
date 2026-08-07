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

    IF @IsActive = 0
    BEGIN
        -- Deactivating can never create an overlap — no lock/backstop needed on this branch.
        UPDATE ArticleDiscounts
        SET    IsActive       = 0,
               LastUpdatedUtc = SYSUTCDATETIME(),
               LastUpdatedBy  = @LastUpdatedBy
        WHERE  ArticleDiscountToken = @ArticleDiscountToken;
    END
    ELSE
    BEGIN
        -- Reactivating can resurrect an overlap with a discount created for the same scope while
        -- this one sat inactive — same DB-level backstop as sp_ArticleDiscount_Create/_Update.
        DECLARE @SupplierId INT, @ArticleId INT, @SubFamilyId INT, @FamilyId INT,
                @EffectiveFrom DATE, @EffectiveUntil DATE;

        SELECT @SupplierId = SupplierId, @ArticleId = ArticleId, @SubFamilyId = SubFamilyId, @FamilyId = FamilyId,
               @EffectiveFrom = EffectiveFrom, @EffectiveUntil = EffectiveUntil
        FROM ArticleDiscounts WHERE ArticleDiscountToken = @ArticleDiscountToken;

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
              AND d.ArticleDiscountToken <> @ArticleDiscountToken
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

        UPDATE ArticleDiscounts
        SET    IsActive       = 1,
               LastUpdatedUtc = SYSUTCDATETIME(),
               LastUpdatedBy  = @LastUpdatedBy
        WHERE  ArticleDiscountToken = @ArticleDiscountToken;

        COMMIT TRANSACTION;
    END

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
