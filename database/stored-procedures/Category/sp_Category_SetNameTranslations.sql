SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE sp_Category_SetNameTranslations
    @CategoryToken     uniqueidentifier,
    @NameTranslations  nvarchar(1000) = NULL,
    @LastUpdatedBy     nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryToken = @CategoryToken)
    BEGIN
        RAISERROR('CATEGORY_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF @NameTranslations IS NOT NULL AND ISJSON(@NameTranslations) = 0
    BEGIN
        RAISERROR('INVALID_REQUEST', 16, 1);
        RETURN;
    END

    -- Deliberately no IsSystem guard, unlike sp_Category_Update/sp_Category_SetActive —
    -- translating the display name isn't a structural-identity change (Code/IsActive),
    -- same reasoning as sp_Family_SetNameTranslations/sp_Family_SetDefaultTaxCategory.
    UPDATE Categories
    SET    NameTranslations = @NameTranslations,
           LastUpdatedUtc   = SYSUTCDATETIME(),
           LastUpdatedBy    = @LastUpdatedBy
    WHERE  CategoryToken = @CategoryToken;

    SELECT
        c.CategoryId,
        c.CategoryToken,
        c.Code,
        c.NameTranslations,
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
