CREATE OR ALTER PROCEDURE sp_SubCategory_ExistsByCode
    @Code       NVARCHAR(50),
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(CASE WHEN EXISTS (
        SELECT 1 FROM SubCategories WHERE Code = @Code AND CategoryId = @CategoryId
    ) THEN 1 ELSE 0 END AS BIT);
END
