SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DEPARTMENT - GET BY TOKEN
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Department_GetByToken
(
    @DepartmentToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.DepartmentId, d.DepartmentToken, d.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        d.Name, d.NormalizedName, d.Code, d.IsActive,
        d.CreatedUtc, d.CreatedBy, d.LastUpdatedUtc, d.LastUpdatedBy
    FROM dbo.Departments d
    JOIN dbo.Organizations org ON org.OrganizationId = d.OrganizationId
    WHERE d.DepartmentToken = @DepartmentToken;
END;
GO
