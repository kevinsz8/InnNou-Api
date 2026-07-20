SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE sp_Category_Create
    @CategoryToken  uniqueidentifier,
    @Code           varchar(100),
    @OrganizationId int = NULL,
    @CreatedBy      nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF @OrganizationId IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM Organizations WHERE OrganizationId = @OrganizationId AND IsDeleted = 0 AND IsActive = 1
    )
    BEGIN
        RAISERROR('CATEGORY_ORGANIZATION_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (
        SELECT 1 FROM Categories
        WHERE Code = @Code
          AND ((@OrganizationId IS NULL AND OrganizationId IS NULL) OR OrganizationId = @OrganizationId)
    )
    BEGIN
        RAISERROR('CATEGORY_CODE_EXISTS', 16, 1);
        RETURN;
    END

    INSERT INTO Categories (CategoryToken, Code, OrganizationId, IsSystem, IsActive, CreatedUtc, CreatedBy)
    VALUES (@CategoryToken, @Code, @OrganizationId, 0, 1, SYSUTCDATETIME(), @CreatedBy);

    SELECT
        c.CategoryId,
        c.CategoryToken,
        c.Code,
        c.OrganizationId,
        c.IsSystem,
        c.IsActive,
        c.CreatedUtc,
        c.CreatedBy,
        c.LastUpdatedUtc,
        c.LastUpdatedBy,
        o.OrganizationToken AS OrganizationTokenResult,
        o.Name AS OrganizationName
    FROM Categories c
    LEFT JOIN Organizations o ON o.OrganizationId = c.OrganizationId
    WHERE c.CategoryToken = @CategoryToken;
END;
GO
