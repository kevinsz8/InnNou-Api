/* =============================================================
   SUBFAMILY - GET BY CODE
   Returns a single active sub-family by (FamilyId, Code) — Code is
   only unique within a Family (UX_SubFamilies), not globally, so
   both parameters are required to resolve unambiguously. Used by
   Article bulk import to resolve Excel "FamilyCode"+"SubFamilyCode"
   columns to a SubFamilyId.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_SubFamily_GetByCode
    @FamilyId INT,
    @Code     VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SubFamilyId,
        SubFamilyToken,
        FamilyId,
        Code,
        IsSystem,
        IsActive
    FROM SubFamilies
    WHERE FamilyId = @FamilyId
      AND Code = @Code
      AND IsActive = 1;
END;
GO
