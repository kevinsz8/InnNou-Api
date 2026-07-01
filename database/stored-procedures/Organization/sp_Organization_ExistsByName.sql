SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION - EXISTS BY NAME
   Returns 1 if a non-deleted organization with the given
   normalized name already exists, 0 otherwise. Used for
   uniqueness checks before create.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Organization_ExistsByName
(
    @NormalizedName VARCHAR(200)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(COUNT(1) AS INT)
    FROM dbo.Organizations
    WHERE NormalizedName = @NormalizedName
      AND IsDeleted = 0;
END;
GO
