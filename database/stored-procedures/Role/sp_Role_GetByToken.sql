/* =============================================================
   ROLE - GET BY TOKEN
   Returns a single active role by its token. Enforces level
   visibility: only returns if RoleLevel <= @MaxLevel so callers
   cannot inspect roles above their own permission level.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Role_GetByToken
(
    @RoleToken UNIQUEIDENTIFIER,
    @MaxLevel  INT
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
    WHERE RoleToken  = @RoleToken
      AND IsActive   = 1
      AND RoleLevel <= @MaxLevel;
END;
GO
