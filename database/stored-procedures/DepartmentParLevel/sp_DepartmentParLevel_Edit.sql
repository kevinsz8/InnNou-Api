SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DEPARTMENTPARLEVEL - EDIT
   Update Minimum/ReorderQuantity for an existing base par level.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_DepartmentParLevel_Edit
(
    @DepartmentParLevelToken UNIQUEIDENTIFIER,
    @MinimumQuantity         DECIMAL(18,8),
    @ReorderQuantity         DECIMAL(18,8),
    @LastUpdatedBy           VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.DepartmentParLevels
    SET MinimumQuantity = @MinimumQuantity,
        ReorderQuantity = @ReorderQuantity,
        LastUpdatedUtc  = SYSUTCDATETIME(),
        LastUpdatedBy   = @LastUpdatedBy
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
