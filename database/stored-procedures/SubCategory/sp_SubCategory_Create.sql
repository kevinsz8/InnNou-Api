CREATE OR ALTER PROCEDURE sp_SubCategory_Create
    @SubCategoryToken uniqueidentifier,
    @CategoryId       int,
    @Code             varchar(100),
    @CreatedBy        nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryId = @CategoryId AND IsActive = 1)
    BEGIN
        RAISERROR('CATEGORY_NOT_FOUND', 16, 1);
        RETURN;
    END

    -- Code must be unique within the same category
    IF EXISTS (SELECT 1 FROM SubCategories WHERE CategoryId = @CategoryId AND Code = @Code)
    BEGIN
        RAISERROR('SUB_CATEGORY_CODE_EXISTS', 16, 1);
        RETURN;
    END

    INSERT INTO SubCategories (SubCategoryToken, CategoryId, Code, IsSystem, IsActive, CreatedUtc, CreatedBy)
    VALUES (@SubCategoryToken, @CategoryId, @Code, 0, 1, SYSUTCDATETIME(), @CreatedBy);

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
