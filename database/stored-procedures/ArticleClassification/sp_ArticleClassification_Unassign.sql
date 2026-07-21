CREATE OR ALTER PROCEDURE sp_ArticleClassification_Unassign
    @ArticleId      INT,
    @OrganizationId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Scoped to exactly the caller's own (ArticleId, OrganizationId) — same structural
    -- guarantee as sp_ArticleFavorite_Delete: there is no parameter path to reach an
    -- ancestor's row, so a descendant can never clear a parent Super Asociado's classification
    -- from here.
    DELETE FROM ArticleClassifications WHERE ArticleId = @ArticleId AND OrganizationId = @OrganizationId;

    SELECT @@ROWCOUNT AS DeletedCount;
END;
GO
