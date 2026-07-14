/* =============================================================
   SUBCATEGORY - GET BY CODE
   Returns a single active sub-category by (CategoryId, Code) — Code
   is only unique within a Category (UX_SubCategories), not globally,
   so both parameters are required to resolve unambiguously. Used by
   SubCategory bulk import to resolve Excel "CategoryCode"+"Code"
   columns to a SubCategoryId.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_SubCategory_GetByCode
    @CategoryId INT,
    @Code       VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SubCategoryId,
        SubCategoryToken,
        CategoryId,
        Code,
        IsSystem,
        IsActive
    FROM SubCategories
    WHERE CategoryId = @CategoryId
      AND Code = @Code
      AND IsActive = 1;
END;
GO
