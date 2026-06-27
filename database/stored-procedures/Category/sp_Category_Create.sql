CREATE OR ALTER PROCEDURE sp_Category_Create
    @CategoryToken uniqueidentifier,
    @Code          varchar(100),
    @CreatedBy     nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Categories WHERE Code = @Code)
    BEGIN
        RAISERROR('CATEGORY_CODE_EXISTS', 16, 1);
        RETURN;
    END

    INSERT INTO Categories (CategoryToken, Code, IsSystem, IsActive, CreatedUtc, CreatedBy)
    VALUES (@CategoryToken, @Code, 0, 1, SYSUTCDATETIME(), @CreatedBy);

    SELECT
        CategoryId,
        CategoryToken,
        Code,
        IsSystem,
        IsActive,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy
    FROM Categories
    WHERE CategoryToken = @CategoryToken;
END;
