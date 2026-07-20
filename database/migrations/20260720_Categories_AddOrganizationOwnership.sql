SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: Categories organization ownership (Super Asociado anchoring)
   Adds Categories.OrganizationId (nullable FK -> Organizations).
   NULL = global/system category (unchanged, 100% of existing rows).
   Set = anchored to exactly one Super Asociado organization, immutable
   after create; its own descendant Asociado organizations get
   read-only inherited visibility (see sp_Category_GetPaged's
   ascending-walk), never their own taxonomy.

   Replaces the single global UQ_Categories_Code unique index with two
   filtered unique indexes (same idiom as ArticlePrices'
   UX_ArticlePrices_Global / UX_ArticlePrices_Contract):
     - UX_Categories_Global       (Code)                WHERE OrganizationId IS NULL
     - UX_Categories_Organization (OrganizationId, Code) WHERE OrganizationId IS NOT NULL

   No schema change on SubCategories — ownership/visibility is derived
   entirely by joining to the parent Categories.OrganizationId.
   Idempotent — safe to re-run.
   ============================================================= */

IF COL_LENGTH('Categories', 'OrganizationId') IS NULL
BEGIN
    ALTER TABLE Categories ADD OrganizationId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Categories_Organizations_OrganizationId')
BEGIN
    ALTER TABLE Categories ADD CONSTRAINT FK_Categories_Organizations_OrganizationId
        FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Categories_OrganizationId' AND object_id = OBJECT_ID('Categories'))
BEGIN
    CREATE INDEX IX_Categories_OrganizationId ON Categories (OrganizationId);
END
GO

-- The original single-column unique constraint on Categories(Code) is looked up by its
-- actual definition, not a fixed name — it was created without an explicit constraint
-- name in at least one environment, so it carries a system-generated name (e.g.
-- UQ__Categori__xxxxxxxx) rather than the UQ_Categories_Code the original snapshot
-- documented. A literal `DROP INDEX UQ_Categories_Code` silently no-ops when the real
-- name differs, leaving the old global-only constraint active alongside the two new
-- filtered indexes and incorrectly blocking cross-partition Code reuse.
DECLARE @OldUniqueConstraintName SYSNAME;
SELECT @OldUniqueConstraintName = kc.name
FROM sys.key_constraints kc
JOIN sys.indexes i ON i.object_id = kc.parent_object_id AND i.index_id = kc.unique_index_id
WHERE kc.parent_object_id = OBJECT_ID('Categories')
  AND kc.type = 'UQ'
  AND i.has_filter = 0
  AND EXISTS (
      SELECT 1 FROM sys.index_columns ic
      JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
      WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND c.name = 'Code'
  )
  AND (SELECT COUNT(*) FROM sys.index_columns ic2 WHERE ic2.object_id = i.object_id AND ic2.index_id = i.index_id) = 1;

IF @OldUniqueConstraintName IS NOT NULL
BEGIN
    DECLARE @DropConstraintSql NVARCHAR(400) = N'ALTER TABLE Categories DROP CONSTRAINT ' + QUOTENAME(@OldUniqueConstraintName) + N';';
    EXEC sp_executesql @DropConstraintSql;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Categories_Global' AND object_id = OBJECT_ID('Categories'))
BEGIN
    CREATE UNIQUE INDEX UX_Categories_Global ON Categories (Code) WHERE OrganizationId IS NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Categories_Organization' AND object_id = OBJECT_ID('Categories'))
BEGIN
    CREATE UNIQUE INDEX UX_Categories_Organization ON Categories (OrganizationId, Code) WHERE OrganizationId IS NOT NULL;
END
GO

PRINT '=== Migration 20260720_Categories_AddOrganizationOwnership completed successfully ===';
GO
