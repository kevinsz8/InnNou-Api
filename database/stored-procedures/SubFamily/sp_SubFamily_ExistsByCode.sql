CREATE OR ALTER PROCEDURE sp_SubFamily_ExistsByCode
    @Code     NVARCHAR(50),
    @FamilyId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(CASE WHEN EXISTS (
        SELECT 1 FROM SubFamilies WHERE Code = @Code AND FamilyId = @FamilyId
    ) THEN 1 ELSE 0 END AS BIT);
END
