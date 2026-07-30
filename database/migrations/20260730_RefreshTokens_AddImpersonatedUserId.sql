-- =============================================================
-- MIGRATION: Add ImpersonatedUserId to RefreshTokens
-- Date: 2026-07-30
-- =============================================================
-- Impersonation's refresh token is stored under the actor's own UserId
-- (sp_Auth_InsertRefreshToken called from AuthService.ImpersonateAsync),
-- so a token refresh mid-impersonation session had no way to know a session
-- was impersonating anyone — RefreshTokenAsync always re-minted the JWT for
-- the refresh token's own owner (the actor), silently dropping back to the
-- actor's real identity with none of the impersonation claims, with no
-- error and no notice to the user. This column lets a refresh preserve the
-- active impersonation across token rotation instead of dropping it.
--
-- NULL = a normal (non-impersonating) session's refresh token, the common
-- case. Set only for a refresh token minted while impersonating, and
-- propagated forward on every subsequent rotation of that same token chain.
--
-- Idempotent — safe to re-run.
-- =============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('RefreshTokens') AND name = 'ImpersonatedUserId'
)
BEGIN
    ALTER TABLE RefreshTokens ADD ImpersonatedUserId INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RefreshTokens_ImpersonatedUser'
)
BEGIN
    ALTER TABLE RefreshTokens
        ADD CONSTRAINT FK_RefreshTokens_ImpersonatedUser
        FOREIGN KEY (ImpersonatedUserId) REFERENCES Users (UserId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_ImpersonatedUserId'
)
BEGIN
    CREATE INDEX IX_RefreshTokens_ImpersonatedUserId ON RefreshTokens (ImpersonatedUserId);
END
GO

PRINT '=== Migration 20260730_RefreshTokens_AddImpersonatedUserId completed successfully ===';
GO
