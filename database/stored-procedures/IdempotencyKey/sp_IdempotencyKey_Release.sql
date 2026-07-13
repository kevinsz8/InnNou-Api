SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   IDEMPOTENCYKEY - RELEASE
   Deletes a Pending row. Called when the wrapped command failed or
   threw, so a client retry with the same key isn't blocked for the
   whole TTL just because the first attempt didn't succeed — only
   genuinely successful mutations get cached-and-replayed.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_IdempotencyKey_Release
(
    @Key         VARCHAR(255),
    @RequestType VARCHAR(300),
    @UserToken   UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.IdempotencyKeys
    WHERE [Key] = @Key AND RequestType = @RequestType AND UserToken = @UserToken AND Status = 'Pending';
END;
GO
