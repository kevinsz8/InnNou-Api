CREATE OR ALTER PROCEDURE sp_Article_ExistsBySupplierSku
    @SupplierId   INT,
    @SupplierSku  VARCHAR(100),
    @ExcludeToken UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- A superseded article (ReplacedByArticleId IS NOT NULL) no longer "owns" its SKU for
    -- uniqueness purposes — Supersede deliberately carries the same SupplierSku forward onto
    -- its replacement (same product line, only the structure changed), so the predecessor must
    -- be excluded here or every second-generation Supersede/Edit with an unchanged SKU would
    -- falsely collide with its own now-inactive ancestor.
    SELECT CAST(CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END AS BIT)
    FROM   Articles
    WHERE  SupplierId          = @SupplierId
      AND  SupplierSku         = @SupplierSku
      AND  IsDeleted            = 0
      AND  ReplacedByArticleId IS NULL
      AND  (@ExcludeToken IS NULL OR ArticleToken <> @ExcludeToken);
END;
