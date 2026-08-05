SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DEPARTMENTPARLEVEL - GET BY DEPARTMENT AND ARTICLE
   Single-row lookup for the base par level, same "no row = not configured"
   convention as sp_ParLevel_GetByWarehouseAndArticle. Used before Create
   to reject a duplicate (DEPARTMENT_PAR_LEVEL_ALREADY_EXISTS).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_DepartmentParLevel_GetByDepartmentAndArticle
(
    @DepartmentId INT,
    @ArticleId    INT
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
    WHERE dpl.DepartmentId = @DepartmentId AND dpl.ArticleId = @ArticleId;
END;
GO
