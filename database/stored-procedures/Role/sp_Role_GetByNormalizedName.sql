/* =============================================================
   ROLE - GET BY NORMALIZED NAME
   Returns a single active role by its normalized name (e.g. the
   seeded 'SUPPLIER' role used to create Supplier shadow Users).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Role_GetByNormalizedName
(
    @NormalizedName VARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        RoleId,
        RoleToken,
        Name,
        NormalizedName,
        Description,
        RoleLevel,
        CanImpersonate,
        IsActive
    FROM dbo.Roles
    WHERE NormalizedName = @NormalizedName
      AND IsActive = 1;
END;
GO
