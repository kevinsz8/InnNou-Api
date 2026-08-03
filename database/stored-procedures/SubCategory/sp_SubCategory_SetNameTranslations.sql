CREATE OR ALTER PROCEDURE sp_SubCategory_SetNameTranslations
    @SubCategoryToken  uniqueidentifier,
    @NameTranslations  nvarchar(1000) = NULL,
    @LastUpdatedBy     nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM SubCategories WHERE SubCategoryToken = @SubCategoryToken)
    BEGIN
        RAISERROR('SUB_CATEGORY_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF @NameTranslations IS NOT NULL AND ISJSON(@NameTranslations) = 0
    BEGIN
        RAISERROR('INVALID_REQUEST', 16, 1);
        RETURN;
    END

    -- Deliberately no IsSystem guard, unlike sp_SubCategory_Update/sp_SubCategory_SetActive —
    -- translating the display name isn't a structural-identity change, same reasoning as
    -- sp_Category_SetNameTranslations.
    UPDATE SubCategories
    SET    NameTranslations = @NameTranslations,
           LastUpdatedUtc   = SYSUTCDATETIME(),
           LastUpdatedBy    = @LastUpdatedBy
    WHERE  SubCategoryToken = @SubCategoryToken;

    SELECT
        sc.SubCategoryId,
        sc.SubCategoryToken,
        sc.CategoryId,
        sc.Code,
        sc.NameTranslations,
        sc.IsSystem,
        sc.IsActive,
        sc.CreatedUtc,
        sc.CreatedBy,
        sc.LastUpdatedUtc,
        sc.LastUpdatedBy,
        c.OrganizationId,
        o.OrganizationToken AS OrganizationTokenResult,
        o.Name AS OrganizationName
    FROM SubCategories sc
    JOIN Categories c ON c.CategoryId = sc.CategoryId
    LEFT JOIN Organizations o ON o.OrganizationId = c.OrganizationId
    WHERE sc.SubCategoryToken = @SubCategoryToken;
END;
GO
