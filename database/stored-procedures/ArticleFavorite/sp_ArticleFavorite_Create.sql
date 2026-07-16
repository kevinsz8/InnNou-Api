SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- ArticleFavorites has a unique index (UX_ArticleFavorites_Article_Organization) — INSERT
-- against a table with a unique index requires QUOTED_IDENTIFIER ON at the session that
-- created this procedure (SQL Server compiles that setting into the proc), not just at index
-- creation time. Without this, every insert fails with error 1934.
CREATE OR ALTER PROCEDURE sp_ArticleFavorite_Create
    @ArticleFavoriteToken UNIQUEIDENTIFIER,
    @ArticleId            INT,
    @OrganizationId       INT,
    @CreatedBy            VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM ArticleFavorites WHERE ArticleId = @ArticleId AND OrganizationId = @OrganizationId)
            INSERT INTO ArticleFavorites (ArticleFavoriteToken, ArticleId, OrganizationId, CreatedBy)
            VALUES (@ArticleFavoriteToken, @ArticleId, @OrganizationId, @CreatedBy);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
    END CATCH

    -- Re-select by (ArticleId, OrganizationId), not @ArticleFavoriteToken — if the row already
    -- existed, the passed-in token was never used; the caller must get back the real,
    -- pre-existing token. This is what makes "mark twice" a no-op success instead of a conflict.
    SELECT
        af.ArticleFavoriteId, af.ArticleFavoriteToken,
        af.ArticleId,      a.ArticleToken, a.Name AS ArticleName, a.SupplierSku,
        s.Name             AS SupplierName,
        af.OrganizationId, o.OrganizationToken, o.Name AS OrganizationName,
        CAST(0 AS BIT)     AS IsInherited,
        af.CreatedUtc, af.CreatedBy
    FROM ArticleFavorites af
    JOIN Articles      a ON a.ArticleId      = af.ArticleId
    JOIN Suppliers     s ON s.SupplierId     = a.SupplierId
    JOIN Organizations o ON o.OrganizationId = af.OrganizationId
    WHERE af.ArticleId = @ArticleId AND af.OrganizationId = @OrganizationId;
END;
GO
