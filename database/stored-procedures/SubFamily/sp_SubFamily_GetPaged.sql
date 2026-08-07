CREATE OR ALTER PROCEDURE sp_SubFamily_GetPaged
(
    @PageNumber      INT,
    @PageSize        INT,
    @FamilyId        INT          = NULL,
    @SearchText      VARCHAR(200) = NULL,
    @IncludeInactive BIT          = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SubFamilyId,
        SubFamilyToken,
        FamilyId,
        Code,
        NameTranslations,
        IsSystem,
        IsActive,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM SubFamilies
    WHERE (@IncludeInactive = 1 OR IsActive = 1)
      AND (@FamilyId IS NULL OR FamilyId = @FamilyId)
      -- Code's column collation is SQL_Latin1_General_CP1_CI_AS (case-insensitive) — confirmed live
      -- against InnNou/InnNou_Test via sys.columns before dropping LOWER() on both sides here, since
      -- wrapping the filtered column in a function blocks an index seek (SARGability).
      AND (@SearchText IS NULL OR Code LIKE '%' + @SearchText + '%')
    ORDER BY FamilyId, Code
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
