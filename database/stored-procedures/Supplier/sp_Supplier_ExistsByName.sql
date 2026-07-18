/* =============================================================
   SUPPLIER - EXISTS BY NAME
   Returns 1 if a supplier with the given normalized name already
   exists WITHIN THE CALLER'S RELEVANT SCOPE, 0 otherwise. Used for
   uniqueness checks before create/edit. Scope-aware (see InnNou-Api
   CLAUDE.md, "Supplier global/private scoping"): a global name must
   be unique among other globals; a private name must be unique
   among globals UNION that exact owning organization's own private
   suppliers (exact-owner match, not hierarchy-wide — two different
   Asociados may each use the same private-supplier name without
   colliding). @ExcludeSupplierId lets an edit re-check exclude the
   row being edited itself.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Supplier_ExistsByName
(
    @NormalizedName    VARCHAR(200),
    @IsGlobal          BIT,
    @OrganizationId    INT = NULL,
    @ExcludeSupplierId INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(COUNT(1) AS INT)
    FROM dbo.Suppliers s
    WHERE s.NormalizedName = @NormalizedName
      AND s.IsDeleted = 0
      AND (@ExcludeSupplierId IS NULL OR s.SupplierId <> @ExcludeSupplierId)
      AND
      (
          (@IsGlobal = 1 AND s.IsGlobal = 1)
          OR
          (
              @IsGlobal = 0
              AND
              (
                  s.IsGlobal = 1
                  OR EXISTS (
                      SELECT 1 FROM dbo.OrganizationSuppliers os
                      WHERE os.SupplierId = s.SupplierId
                        AND os.IsActive = 1
                        AND os.OrganizationId = @OrganizationId
                  )
              )
          )
      );
END;
GO
