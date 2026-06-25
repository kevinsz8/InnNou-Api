/* =============================================================
   AUTH - END IMPERSONATION SESSION
   Closes all open impersonation sessions for the given actor.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Auth_EndImpersonationSession
(
    @ActorUserId INT,
    @EndedUtc    DATETIME2(7)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.ImpersonationSessions
    SET
        EndedUtc = @EndedUtc
    WHERE ActorUserId = @ActorUserId
      AND EndedUtc IS NULL;
END;
GO
