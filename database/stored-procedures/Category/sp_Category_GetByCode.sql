/* =============================================================
   CATEGORY - GET BY CODE
   Returns a single active category by its Code (globally unique via
   UQ_Categories_Code). Used by Category bulk import to resolve an
   Excel "Code" row's duplicate check, and by SubCategory bulk
   import to resolve its "CategoryCode" column to a CategoryId.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_Category_GetByCode
    @Code VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CategoryId,
        CategoryToken,
        Code,
        IsSystem,
        IsActive
    FROM Categories
    WHERE Code = @Code
      AND IsActive = 1;
END;
GO
