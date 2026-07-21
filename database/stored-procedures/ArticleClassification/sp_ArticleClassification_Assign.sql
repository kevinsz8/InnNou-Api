SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- ArticleClassifications has a unique index (UX_ArticleClassifications_Article_Organization) —
-- writes against a table with a unique index require QUOTED_IDENTIFIER ON at the session that
-- created this procedure (SQL Server compiles that setting into the proc), not just at index
-- creation time. Without this, every insert/update fails with error 1934.
--
-- Upsert by (ArticleId, OrganizationId): insert if the org has never classified this article,
-- else update in place (unlike ArticleFavorites' pure toggle, a classification is reassigned,
-- not re-created) — mirrors sp_ArticleFavorite_Create's "idempotent success" shape but supports
-- changing Category/SubCategory on an existing row instead of only no-op'ing.
CREATE OR ALTER PROCEDURE sp_ArticleClassification_Assign
    @ArticleClassificationToken UNIQUEIDENTIFIER,
    @ArticleId                  INT,
    @OrganizationId             INT,
    @CategoryId                 INT,
    @SubCategoryId              INT          = NULL,
    @CreatedBy                  VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM ArticleClassifications WHERE ArticleId = @ArticleId AND OrganizationId = @OrganizationId)
    BEGIN
        UPDATE ArticleClassifications
        SET CategoryId     = @CategoryId,
            SubCategoryId  = @SubCategoryId,
            LastUpdatedUtc = SYSUTCDATETIME(),
            LastUpdatedBy  = @CreatedBy
        WHERE ArticleId = @ArticleId AND OrganizationId = @OrganizationId;
    END
    ELSE
    BEGIN
        INSERT INTO ArticleClassifications (ArticleClassificationToken, ArticleId, OrganizationId, CategoryId, SubCategoryId, CreatedBy)
        VALUES (@ArticleClassificationToken, @ArticleId, @OrganizationId, @CategoryId, @SubCategoryId, @CreatedBy);
    END

    SELECT
        ac.ArticleClassificationId, ac.ArticleClassificationToken,
        ac.ArticleId,      a.ArticleToken, a.Name AS ArticleName, a.SupplierSku,
        s.Name             AS SupplierName,
        ac.OrganizationId, o.OrganizationToken, o.Name AS OrganizationName,
        ac.CategoryId,     c.CategoryToken, c.Code AS CategoryCode,
        ac.SubCategoryId,  sc.SubCategoryToken, sc.Code AS SubCategoryCode,
        CAST(0 AS BIT)     AS IsInherited,
        ac.CreatedUtc, ac.CreatedBy, ac.LastUpdatedUtc, ac.LastUpdatedBy
    FROM ArticleClassifications ac
    JOIN Articles      a  ON a.ArticleId      = ac.ArticleId
    JOIN Suppliers     s  ON s.SupplierId     = a.SupplierId
    JOIN Organizations o  ON o.OrganizationId = ac.OrganizationId
    JOIN Categories    c  ON c.CategoryId     = ac.CategoryId
    LEFT JOIN SubCategories sc ON sc.SubCategoryId = ac.SubCategoryId
    WHERE ac.ArticleId = @ArticleId AND ac.OrganizationId = @OrganizationId;
END;
GO
