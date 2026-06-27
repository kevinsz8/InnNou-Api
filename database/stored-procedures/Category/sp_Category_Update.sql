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

    IF EXISTS (SELECT 1 FROM Categories WHERE Code = @Code AND CategoryToken <> @CategoryToken)
    BEGIN
        RAISERROR('CATEGORY_CODE_EXISTS', 16, 1);
        RETURN;
    END

    UPDATE Categories
    SET    Code          = @Code,
           LastUpdatedUtc = SYSUTCDATETIME(),
           LastUpdatedBy  = @LastUpdatedBy
    WHERE  CategoryToken = @CategoryToken;

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
