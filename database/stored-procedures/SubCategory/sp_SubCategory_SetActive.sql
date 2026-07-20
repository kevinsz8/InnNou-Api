CREATE OR ALTER PROCEDURE sp_SubCategory_SetActive
    @SubCategoryToken uniqueidentifier,
    @IsActive         bit,
    @LastUpdatedBy    nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM SubCategories WHERE SubCategoryToken = @SubCategoryToken)
    BEGIN
        RAISERROR('SUB_CATEGORY_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM SubCategories WHERE SubCategoryToken = @SubCategoryToken AND IsSystem = 1)
    BEGIN
        RAISERROR('SUB_CATEGORY_SYSTEM_READONLY', 16, 1);
        RETURN;
    END

    UPDATE SubCategories
    SET    IsActive       = @IsActive,
           LastUpdatedUtc = SYSUTCDATETIME(),
           LastUpdatedBy  = @LastUpdatedBy
    WHERE  SubCategoryToken = @SubCategoryToken;

    SELECT
        sc.SubCategoryId,
        sc.SubCategoryToken,
        sc.CategoryId,
        sc.Code,
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
