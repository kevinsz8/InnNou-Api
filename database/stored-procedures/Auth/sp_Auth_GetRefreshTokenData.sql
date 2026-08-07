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
        rt.ImpersonatedUserId,

        u.UserToken,
        u.Email,
        u.OrganizationId,
        u.SupplierId,
        u.IsActive,
        u.IsDeleted,

        r.RoleLevel,
        r.CanImpersonate,
        ot.Code AS OrganizationTypeCode,
        wc.WarehouseId,

        -- Populated only when this refresh token was minted mid-impersonation
        -- (ImpersonatedUserId set) — lets RefreshTokenAsync re-mint the JWT for
        -- the same impersonated target instead of silently reverting to the actor.
        iu.UserToken AS ImpersonatedUserToken,
        iu.Email AS ImpersonatedEmail,
        iu.OrganizationId AS ImpersonatedOrganizationId,
        iu.SupplierId AS ImpersonatedSupplierId,
        ir.RoleLevel AS ImpersonatedRoleLevel,
        iot.Code AS ImpersonatedOrganizationTypeCode,
        iwc.WarehouseId AS ImpersonatedWarehouseId,
        iu.IsActive AS ImpersonatedIsActive,
        iu.IsDeleted AS ImpersonatedIsDeleted
    FROM dbo.RefreshTokens rt
    INNER JOIN dbo.Users u ON u.UserId = rt.UserId
    INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
    LEFT JOIN dbo.Organizations o ON o.OrganizationId = u.OrganizationId
    LEFT JOIN dbo.OrganizationTypes ot ON ot.OrganizationTypeId = o.OrganizationTypeId
    LEFT JOIN dbo.WarehouseContacts wc ON wc.WarehouseContactId = u.WarehouseContactId
    LEFT JOIN dbo.Users iu ON iu.UserId = rt.ImpersonatedUserId
    LEFT JOIN dbo.Roles ir ON ir.RoleId = iu.RoleId
    LEFT JOIN dbo.Organizations io ON io.OrganizationId = iu.OrganizationId
    LEFT JOIN dbo.OrganizationTypes iot ON iot.OrganizationTypeId = io.OrganizationTypeId
    LEFT JOIN dbo.WarehouseContacts iwc ON iwc.WarehouseContactId = iu.WarehouseContactId
    WHERE rt.TokenHash = @TokenHash;
END;
GO
