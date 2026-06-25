/* =============================================================
   AUTH - ON LOGIN FAILURE
   Increments the failed login counter and locks the account
   for 15 minutes after 5 consecutive failures.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Auth_OnLoginFailure
(
    @UserId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET
        FailedLoginCount = FailedLoginCount + 1,
        LockedUntilUtc   =
            CASE
                WHEN FailedLoginCount + 1 >= 5
                    THEN DATEADD(MINUTE, 15, SYSUTCDATETIME())
                ELSE LockedUntilUtc
            END,
        LastUpdatedUtc   = SYSUTCDATETIME(),
        LastUpdatedBy    = 'System'
    WHERE UserId = @UserId;
END;
GO
