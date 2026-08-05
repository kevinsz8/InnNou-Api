SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DEPARTMENTPARLEVEL - CREATE
   Base par level for a (Department, Article) pair. Plain insert + re-select,
   RequisitionService checks UX_DepartmentParLevels_Department_Article via
   sp_DepartmentParLevel_GetByDepartmentAndArticle before calling this, same
   check-then-insert shape as sp_ParLevel_Create.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_DepartmentParLevel_Create
(
    @DepartmentParLevelToken UNIQUEIDENTIFIER,
    @DepartmentId            INT,
    @ArticleId               INT,
    @MinimumQuantity         DECIMAL(18,8),
    @ReorderQuantity         DECIMAL(18,8),
    @CreatedBy               VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.DepartmentParLevels (DepartmentParLevelToken, DepartmentId, ArticleId, MinimumQuantity, ReorderQuantity, CreatedBy)
    VALUES (@DepartmentParLevelToken, @DepartmentId, @ArticleId, @MinimumQuantity, @ReorderQuantity, @CreatedBy);

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
