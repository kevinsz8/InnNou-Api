SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: ArticleClassifications (Article <-> Category/SubCategory)
   Per-organization override, current-state + audit shape (mirrors
   ArticleFavorites, not ArticlePrices' insert-only log) — a shared
   Article can be classified differently by two unrelated Super
   Asociado organizations, so this can never be a column on Articles
   itself.

   OrganizationId is always a SUPER_ASSOCIATE-typed organization,
   enforced app-side (ArticleClassificationService), same as
   Categories.OrganizationId. SubCategoryId is nullable — an article
   may be classified at Category level only.

   Historical BI protection lives elsewhere: OrderLine/PurchaseOrderLine
   snapshot the resolved CategoryName/SubCategoryName as frozen text at
   line-add time (see 20260723_OrderLines_AddClassificationSnapshot.sql)
   so a later reassignment/rename here never changes a past report.

   Idempotent — safe to re-run.
   ============================================================= */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ArticleClassifications')
BEGIN
    CREATE TABLE ArticleClassifications
    (
        ArticleClassificationId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ArticleClassificationToken UNIQUEIDENTIFIER   NOT NULL DEFAULT NEWID(),
        ArticleId                  INT                NOT NULL,
        OrganizationId             INT                NOT NULL,
        CategoryId                 INT                NOT NULL,
        SubCategoryId              INT                NULL,
        CreatedUtc                 DATETIME2          NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                  VARCHAR(150)       NULL,
        LastUpdatedUtc             DATETIME2          NULL,
        LastUpdatedBy              VARCHAR(150)       NULL,

        CONSTRAINT FK_ArticleClassifications_Articles_ArticleId
            FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId),
        CONSTRAINT FK_ArticleClassifications_Organizations_OrganizationId
            FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId),
        CONSTRAINT FK_ArticleClassifications_Categories_CategoryId
            FOREIGN KEY (CategoryId) REFERENCES Categories (CategoryId),
        CONSTRAINT FK_ArticleClassifications_SubCategories_SubCategoryId
            FOREIGN KEY (SubCategoryId) REFERENCES SubCategories (SubCategoryId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ArticleClassifications_Article_Organization' AND object_id = OBJECT_ID('ArticleClassifications'))
BEGIN
    CREATE UNIQUE INDEX UX_ArticleClassifications_Article_Organization ON ArticleClassifications (ArticleId, OrganizationId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ArticleClassifications_OrganizationId' AND object_id = OBJECT_ID('ArticleClassifications'))
BEGIN
    CREATE INDEX IX_ArticleClassifications_OrganizationId ON ArticleClassifications (OrganizationId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ArticleClassifications_CategoryId' AND object_id = OBJECT_ID('ArticleClassifications'))
BEGIN
    CREATE INDEX IX_ArticleClassifications_CategoryId ON ArticleClassifications (CategoryId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ArticleClassifications_SubCategoryId' AND object_id = OBJECT_ID('ArticleClassifications'))
BEGIN
    CREATE INDEX IX_ArticleClassifications_SubCategoryId ON ArticleClassifications (SubCategoryId);
END
GO

PRINT '=== Migration 20260723_ArticleClassifications_Create completed successfully ===';
GO
