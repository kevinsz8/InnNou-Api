/* =============================================================
   AUTH - REVOKE REFRESH TOKEN
   Marks a refresh token as revoked during token rotation or
   explicit logout.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Auth_RevokeRefreshToken
(
    @TokenHash       VARCHAR(500),
    @RevokedUtc      DATETIME2(7),
    @ReplacedByToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.RefreshTokens
    SET
        IsRevoked        = 1,
        RevokedUtc       = @RevokedUtc,
        ReplacedByToken  = @ReplacedByToken
    WHERE TokenHash = @TokenHash
      AND IsRevoked = 0;
END;
GO
