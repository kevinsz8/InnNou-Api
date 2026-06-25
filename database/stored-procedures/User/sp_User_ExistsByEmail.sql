/* =============================================================
   USER - EXISTS BY EMAIL
   Returns 1 if a non-deleted user with the given normalized
   email already exists, 0 otherwise. Used for uniqueness checks
   before create.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_User_ExistsByEmail
(
    @NormalizedEmail VARCHAR(320)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.Users
                WHERE NormalizedEmail = @NormalizedEmail
                  AND IsDeleted = 0
            )
            THEN 1
            ELSE 0
        END;
END;
GO
