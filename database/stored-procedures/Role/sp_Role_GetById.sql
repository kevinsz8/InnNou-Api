/* =============================================================
   ROLE - GET BY ID
   Returns a single active role by its primary key.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Role_GetById
(
    @RoleId INT
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
    WHERE RoleId  = @RoleId
      AND IsActive = 1;
END;
GO
