SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   IDEMPOTENCYKEY - TRY BEGIN
   Attempts to insert a new Pending row for (Key, RequestType, UserToken).
   The unique index UX_IdempotencyKeys_Key is what makes the "two
   concurrent requests, same key" race safe: exactly one INSERT wins,
   the other hits a duplicate-key error (2601/2627) and instead reports
   what's already there.

   Returns exactly one row:
     Outcome            'Inserted' | 'Pending' | 'Completed' | 'HashMismatch'
     ResponseStatusCode  (only set when Outcome = 'Completed')
     ResponseBody        (only set when Outcome = 'Completed')
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_IdempotencyKey_TryBegin
(
    @Key         VARCHAR(255),
    @RequestType VARCHAR(300),
    @UserToken   UNIQUEIDENTIFIER,
    @RequestHash CHAR(64),
    @CreatedUtc  DATETIME2(7),
    @ExpiresUtc  DATETIME2(7)
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        INSERT INTO dbo.IdempotencyKeys
            ([Key], RequestType, UserToken, RequestHash, Status, CreatedUtc, ExpiresUtc)
        VALUES
            (@Key, @RequestType, @UserToken, @RequestHash, 'Pending', @CreatedUtc, @ExpiresUtc);

        SELECT
            'Inserted' AS Outcome,
            CAST(NULL AS INT) AS ResponseStatusCode,
            CAST(NULL AS NVARCHAR(MAX)) AS ResponseBody;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (2601, 2627)
            THROW;

        DECLARE @ExistingStatus VARCHAR(20);
        DECLARE @ExistingHash CHAR(64);
        DECLARE @ExistingResponseStatusCode INT;
        DECLARE @ExistingResponseBody NVARCHAR(MAX);

        SELECT
            @ExistingStatus = Status,
            @ExistingHash = RequestHash,
            @ExistingResponseStatusCode = ResponseStatusCode,
            @ExistingResponseBody = ResponseBody
        FROM dbo.IdempotencyKeys
        WHERE [Key] = @Key AND RequestType = @RequestType AND UserToken = @UserToken;

        IF @ExistingHash <> @RequestHash
            SELECT 'HashMismatch' AS Outcome, CAST(NULL AS INT) AS ResponseStatusCode, CAST(NULL AS NVARCHAR(MAX)) AS ResponseBody;
        ELSE IF @ExistingStatus = 'Completed'
            SELECT 'Completed' AS Outcome, @ExistingResponseStatusCode AS ResponseStatusCode, @ExistingResponseBody AS ResponseBody;
        ELSE
            SELECT 'Pending' AS Outcome, CAST(NULL AS INT) AS ResponseStatusCode, CAST(NULL AS NVARCHAR(MAX)) AS ResponseBody;
    END CATCH
END;
GO
