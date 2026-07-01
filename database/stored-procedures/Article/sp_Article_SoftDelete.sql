CREATE OR ALTER PROCEDURE sp_Article_SoftDelete
    @ArticleToken UNIQUEIDENTIFIER,
    @DeletedBy    VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Articles WHERE ArticleToken = @ArticleToken AND IsDeleted = 0)
    BEGIN
        RAISERROR('ARTICLE_NOT_FOUND', 16, 1);
        RETURN;
    END

    UPDATE Articles
    SET    IsActive    = 0,
           IsDeleted   = 1,
           DeletedUtc  = SYSUTCDATETIME(),
           DeletedBy   = @DeletedBy
    WHERE  ArticleToken = @ArticleToken;
END;
