/* =============================================================
   AUTH - INSERT REFRESH TOKEN
   Persists a new refresh token on login or token rotation.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Auth_InsertRefreshToken
(
    @RefreshTokenToken   UNIQUEIDENTIFIER,
    @UserId              INT,
    @TokenHash           VARCHAR(500),
    @ExpiresUtc          DATETIME2(7),
    @CreatedUtc          DATETIME2(7),
    @ImpersonatedUserId  INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.RefreshTokens
    (
        RefreshTokenToken,
        UserId,
        TokenHash,
        ExpiresUtc,
        CreatedUtc,
        IsRevoked,
        ImpersonatedUserId
    )
    VALUES
    (
        @RefreshTokenToken,
        @UserId,
        @TokenHash,
        @ExpiresUtc,
        @CreatedUtc,
        0,
        @ImpersonatedUserId
    );
END;
GO
