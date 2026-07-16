CREATE OR ALTER PROCEDURE sp_ArticleFavorite_Delete
    @ArticleId      INT,
    @OrganizationId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Scoped to exactly the caller's own (ArticleId, OrganizationId) — there is no parameter
    -- path to reach an ancestor's row, so "a child can't override a parent's favorite" falls
    -- out structurally here, no extra logic needed.
    DELETE FROM ArticleFavorites WHERE ArticleId = @ArticleId AND OrganizationId = @OrganizationId;

    SELECT @@ROWCOUNT AS DeletedCount;
END;
GO
