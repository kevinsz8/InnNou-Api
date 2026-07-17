SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   AUTH - GET REFRESH TOKEN DATA
   Returns refresh token + joined user/role data for token
   rotation validation.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetRefreshTokenData
(
    @TokenHash VARCHAR(500)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rt.RefreshTokenId,
        rt.RefreshTokenToken,
        rt.UserId,
        rt.TokenHash,
        rt.ExpiresUtc,
        rt.IsRevoked,
        rt.RevokedUtc,
        rt.ReplacedByToken,

        u.UserToken,
        u.Email,
        u.OrganizationId,
        u.SupplierId,
        u.IsActive,
        u.IsDeleted,

        r.RoleLevel,
        r.CanImpersonate,
        ot.Code AS OrganizationTypeCode
    FROM dbo.RefreshTokens rt
    INNER JOIN dbo.Users u ON u.UserId = rt.UserId
    INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
    LEFT JOIN dbo.Organizations o ON o.OrganizationId = u.OrganizationId
    LEFT JOIN dbo.OrganizationTypes ot ON ot.OrganizationTypeId = o.OrganizationTypeId
    WHERE rt.TokenHash = @TokenHash;
END;
GO
