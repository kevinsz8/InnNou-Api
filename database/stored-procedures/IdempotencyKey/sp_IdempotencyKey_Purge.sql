SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   IDEMPOTENCYKEY - PURGE
   Deletes rows past their ExpiresUtc. Called periodically by
   IdempotencyKeyCleanupService (a BackgroundService), not by any
   request path.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_IdempotencyKey_Purge
(
    @BeforeUtc DATETIME2(7)
)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.IdempotencyKeys
    WHERE ExpiresUtc < @BeforeUtc;
END;
GO
