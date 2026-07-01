/* =============================================================
   USER - GET BY SUPPLIER ID
   Returns the shadow User row linked to a Supplier (one-to-one),
   looked up by Suppliers.SupplierId. Used when toggling a
   Supplier's HasAccessToSystem flag.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_User_GetBySupplierId
(
    @SupplierId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.Users
    WHERE SupplierId = @SupplierId
      AND IsDeleted = 0;
END;
GO
