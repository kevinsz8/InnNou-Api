/* =============================================================
   HOTEL - EXISTS BY NAME
   Returns 1 if a non-deleted hotel with the given normalized
   name already exists, 0 otherwise. Used for uniqueness checks
   before create.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Hotel_ExistsByName
(
    @NormalizedName VARCHAR(200)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(COUNT(1) AS INT)
    FROM dbo.Hotels
    WHERE NormalizedName = @NormalizedName
      AND IsDeleted = 0;
END;
GO
