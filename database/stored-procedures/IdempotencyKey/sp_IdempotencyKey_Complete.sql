SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   IDEMPOTENCYKEY - COMPLETE
   Flips a Pending row to Completed with the real response, once the
   wrapped command has actually succeeded. Only called on success —
   a failed/thrown command instead calls sp_IdempotencyKey_Release so
   a retry with the same key isn't locked out for the whole TTL.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_IdempotencyKey_Complete
(
    @Key                VARCHAR(255),
    @RequestType        VARCHAR(300),
    @UserToken          UNIQUEIDENTIFIER,
    @ResponseStatusCode INT,
    @ResponseBody       NVARCHAR(MAX),
    @CompletedUtc       DATETIME2(7)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.IdempotencyKeys
    SET
        Status = 'Completed',
        ResponseStatusCode = @ResponseStatusCode,
        ResponseBody = @ResponseBody,
        CompletedUtc = @CompletedUtc
    WHERE [Key] = @Key AND RequestType = @RequestType AND UserToken = @UserToken;
END;
GO
