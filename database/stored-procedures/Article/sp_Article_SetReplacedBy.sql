CREATE OR ALTER PROCEDURE sp_Article_SetReplacedBy
    @ArticleToken        UNIQUEIDENTIFIER,
    @ReplacedByArticleId INT,
    @LastUpdatedBy       VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Articles
    SET    IsActive            = 0,
           ReplacedByArticleId = @ReplacedByArticleId,
           LastUpdatedUtc      = SYSUTCDATETIME(),
           LastUpdatedBy       = @LastUpdatedBy
    WHERE  ArticleToken = @ArticleToken
      AND  IsDeleted    = 0;
END;
