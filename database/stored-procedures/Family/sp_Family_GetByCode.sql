/* =============================================================
   FAMILY - GET BY CODE
   Returns a single active family by its Code (globally unique via
   UQ_Families_Code). Used by Article bulk import to resolve an
   Excel "FamilyCode" column to a FamilyId.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_Family_GetByCode
    @Code VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        FamilyId,
        FamilyToken,
        Code,
        IsSystem,
        IsActive
    FROM Families
    WHERE Code = @Code
      AND IsActive = 1;
END;
GO
