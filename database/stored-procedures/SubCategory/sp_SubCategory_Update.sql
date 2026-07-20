CREATE OR ALTER PROCEDURE sp_SubCategory_Update
    @SubCategoryToken uniqueidentifier,
    @Code             varchar(100),
    @LastUpdatedBy    nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CategoryId int;
    SELECT @CategoryId = CategoryId FROM SubCategories WHERE SubCategoryToken = @SubCategoryToken;

    IF @CategoryId IS NULL
    BEGIN
        RAISERROR('SUB_CATEGORY_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM SubCategories WHERE SubCategoryToken = @SubCategoryToken AND IsSystem = 1)
    BEGIN
        RAISERROR('SUB_CATEGORY_SYSTEM_READONLY', 16, 1);
        RETURN;
    END

    -- Code must be unique within the same category
    IF EXISTS (SELECT 1 FROM SubCategories WHERE CategoryId = @CategoryId AND Code = @Code AND SubCategoryToken <> @SubCategoryToken)
    BEGIN
        RAISERROR('SUB_CATEGORY_CODE_EXISTS', 16, 1);
        RETURN;
    END

    UPDATE SubCategories
    SET    Code           = @Code,
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
