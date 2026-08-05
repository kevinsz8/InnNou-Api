SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DEPARTMENTPARLEVEL - GET BY TOKEN
   Used by Edit/SetActive to resolve the target row and its DepartmentId
   (for authorization) before writing.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_DepartmentParLevel_GetByToken
(
    @DepartmentParLevelToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

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
