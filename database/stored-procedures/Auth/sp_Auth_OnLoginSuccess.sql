/* =============================================================
   AUTH - ON LOGIN SUCCESS
   Resets the failed login counter and records the last login
   timestamp.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Auth_OnLoginSuccess
(
    @UserId       INT,
    @LastLoginUtc DATETIME2(7)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET
        FailedLoginCount = 0,
        LockedUntilUtc   = NULL,
        LastLoginUtc     = @LastLoginUtc,
        LastUpdatedUtc   = SYSUTCDATETIME(),
        LastUpdatedBy    = 'System'
    WHERE UserId = @UserId;
END;
GO
