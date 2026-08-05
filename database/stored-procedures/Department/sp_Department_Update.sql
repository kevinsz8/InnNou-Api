SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DEPARTMENT - UPDATE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Department_Update
(
    @DepartmentToken UNIQUEIDENTIFIER,
    @Name            NVARCHAR(150),
    @NormalizedName  NVARCHAR(150),
    @Code            VARCHAR(20)  = NULL,
    @LastUpdatedBy   VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Departments
    SET Name = @Name,
        NormalizedName = @NormalizedName,
        Code = @Code,
        LastUpdatedUtc = SYSUTCDATETIME(),
        LastUpdatedBy = @LastUpdatedBy
    WHERE DepartmentToken = @DepartmentToken;

    SELECT
        d.DepartmentId, d.DepartmentToken, d.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        d.Name, d.NormalizedName, d.Code, d.IsActive,
        d.CreatedUtc, d.CreatedBy, d.LastUpdatedUtc, d.LastUpdatedBy
    FROM dbo.Departments d
    JOIN dbo.Organizations org ON org.OrganizationId = d.OrganizationId
    WHERE d.DepartmentToken = @DepartmentToken;
END;
GO
