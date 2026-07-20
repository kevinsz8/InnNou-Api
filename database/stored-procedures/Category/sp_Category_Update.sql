SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE sp_Category_Update
    @CategoryToken uniqueidentifier,
    @Code          varchar(100),
    @LastUpdatedBy nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryToken = @CategoryToken)
    BEGIN
        RAISERROR('CATEGORY_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM Categories WHERE CategoryToken = @CategoryToken AND IsSystem = 1)
    BEGIN
        RAISERROR('CATEGORY_SYSTEM_READONLY', 16, 1);
        RETURN;
    END

    DECLARE @ExistingOrganizationId INT;
    SELECT @ExistingOrganizationId = OrganizationId FROM Categories WHERE CategoryToken = @CategoryToken;

    IF EXISTS (
        SELECT 1 FROM Categories
        WHERE Code = @Code
          AND CategoryToken <> @CategoryToken
          AND ((@ExistingOrganizationId IS NULL AND OrganizationId IS NULL) OR OrganizationId = @ExistingOrganizationId)
    )
    BEGIN
        RAISERROR('CATEGORY_CODE_EXISTS', 16, 1);
        RETURN;
    END

    UPDATE Categories
    SET    Code           = @Code,
           LastUpdatedUtc = SYSUTCDATETIME(),
           LastUpdatedBy  = @LastUpdatedBy
    WHERE  CategoryToken = @CategoryToken;

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
