/* =============================================================
   AUTH - INSERT IMPERSONATION SESSION
   Records the start of an impersonation session for audit
   purposes.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Auth_InsertImpersonationSession
(
    @ActorUserId  INT,
    @TargetUserId INT,
    @StartedUtc   DATETIME2(7)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ImpersonationSessions
    (
        ActorUserId,
        TargetUserId,
        StartedUtc
    )
    VALUES
    (
        @ActorUserId,
        @TargetUserId,
        @StartedUtc
    );
END;
GO
