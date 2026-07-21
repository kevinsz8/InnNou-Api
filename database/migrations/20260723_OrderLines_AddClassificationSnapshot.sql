SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: OrderLine/PurchaseOrderLine classification snapshot
   Adds nullable CategoryId/CategoryCode/SubCategoryId/SubCategoryCode
   to both tables. CategoryId/SubCategoryId are traceability-only FKs
   (same convention as OrderLine.ArticleId) — CategoryCode/
   SubCategoryCode are frozen plain text (Category/SubCategory only
   ever carry a Code, no separate Name), resolved once at line-add
   time by OrderService.AddLineAsync (sp_ArticleClassification_
   GetEffectiveForArticle) and copied verbatim onto PurchaseOrderLine
   at Submit time. This is what protects historical spend-by-category
   reporting: a later Article reclassification or a Category Code
   rename must never retroactively change what an already-placed Order
   reports, the same reasoning ArticlePrice/packaging snapshots
   already rely on (see CLAUDE.md's "Historical BI protection").

   An unclassified article simply snapshots NULLs — classification is
   optional metadata, never a purchasing precondition.

   Idempotent — safe to re-run.
   ============================================================= */

IF COL_LENGTH('OrderLine', 'CategoryId') IS NULL
BEGIN
    ALTER TABLE OrderLine ADD CategoryId INT NULL, CategoryCode NVARCHAR(50) NULL,
                              SubCategoryId INT NULL, SubCategoryCode NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderLine_Categories_CategoryId')
BEGIN
    ALTER TABLE OrderLine ADD CONSTRAINT FK_OrderLine_Categories_CategoryId
        FOREIGN KEY (CategoryId) REFERENCES Categories (CategoryId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderLine_SubCategories_SubCategoryId')
BEGIN
    ALTER TABLE OrderLine ADD CONSTRAINT FK_OrderLine_SubCategories_SubCategoryId
        FOREIGN KEY (SubCategoryId) REFERENCES SubCategories (SubCategoryId);
END
GO

IF COL_LENGTH('PurchaseOrderLine', 'CategoryId') IS NULL
BEGIN
    ALTER TABLE PurchaseOrderLine ADD CategoryId INT NULL, CategoryCode NVARCHAR(50) NULL,
                                      SubCategoryId INT NULL, SubCategoryCode NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PurchaseOrderLine_Categories_CategoryId')
BEGIN
    ALTER TABLE PurchaseOrderLine ADD CONSTRAINT FK_PurchaseOrderLine_Categories_CategoryId
        FOREIGN KEY (CategoryId) REFERENCES Categories (CategoryId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PurchaseOrderLine_SubCategories_SubCategoryId')
BEGIN
    ALTER TABLE PurchaseOrderLine ADD CONSTRAINT FK_PurchaseOrderLine_SubCategories_SubCategoryId
        FOREIGN KEY (SubCategoryId) REFERENCES SubCategories (SubCategoryId);
END
GO

PRINT '=== Migration 20260723_OrderLines_AddClassificationSnapshot completed successfully ===';
GO
