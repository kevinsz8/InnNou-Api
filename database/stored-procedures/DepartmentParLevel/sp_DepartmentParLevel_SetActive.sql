SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DEPARTMENTPARLEVEL - SET ACTIVE
   Soft toggle, not a hard delete -- unlike ParLevels/ParLevelOverrides (pure
   operational config with no downstream reference), a DepartmentParLevel's
   own history is worth keeping (e.g. "we used to track this, then stopped")
   and it participates in sp_DepartmentParLevel_GetSuggested's own
   IsActive = 1 filter, so an unwanted suggestion can be turned off without
   losing the configured Minimum/ReorderQuantity values.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_DepartmentParLevel_SetActive
(
    @DepartmentParLevelToken UNIQUEIDENTIFIER,
    @IsActive                BIT,
    @LastUpdatedBy           VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.DepartmentParLevels
    SET IsActive       = @IsActive,
        LastUpdatedUtc = SYSUTCDATETIME(),
        LastUpdatedBy  = @LastUpdatedBy
    WHERE DepartmentParLevelToken = @DepartmentParLevelToken;

    SELECT
        dpl.DepartmentParLevelId, dpl.DepartmentParLevelToken,
        dpl.DepartmentId, d.DepartmentToken, d.Name AS DepartmentName, d.OrganizationId,
        dpl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        dpl.MinimumQuantity, dpl.ReorderQuantity, dpl.IsActive,
        dpl.CreatedUtc, dpl.CreatedBy, dpl.LastUpdatedUtc, dpl.LastUpdatedBy
    FROM dbo.DepartmentParLevels dpl
    JOIN dbo.Departments d ON d.DepartmentId = dpl.DepartmentId
    JOIN dbo.Articles a    ON a.ArticleId    = dpl.ArticleId
    WHERE dpl.DepartmentParLevelToken = @DepartmentParLevelToken;
END;
GO
