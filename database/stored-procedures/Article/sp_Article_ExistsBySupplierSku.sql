CREATE OR ALTER PROCEDURE sp_Article_ExistsBySupplierSku
    @SupplierId   INT,
    @SupplierSku  VARCHAR(100),
    @ExcludeToken UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END AS BIT)
    FROM   Articles
    WHERE  SupplierId  = @SupplierId
      AND  SupplierSku = @SupplierSku
      AND  IsDeleted   = 0
      AND  (@ExcludeToken IS NULL OR ArticleToken <> @ExcludeToken);
END;
