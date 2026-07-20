/* =============================================================
   CATEGORY - GET BY ID
   Internal-only lookup (no ICategoryService method — called via raw
   Dapper from SubCategoryService, same convention as
   sp_OrganizationSupplier_GetActiveBySupplierId). Resolves a parent
   Category's OrganizationId so SubCategory's write-authorization
   check can confirm the caller owns the parent Category before
   creating a SubCategory under it.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_Category_GetById
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryToken, Code, OrganizationId, IsSystem, IsActive
    FROM Categories
    WHERE CategoryId = @CategoryId;
END;
GO
