SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DEPARTMENT - CREATE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Department_Create
(
    @DepartmentToken UNIQUEIDENTIFIER,
    @OrganizationId  INT,
    @Name            NVARCHAR(150),
    @NormalizedName  NVARCHAR(150),
    @Code            VARCHAR(20)  = NULL,
    @CreatedBy       VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Organizations WHERE OrganizationId = @OrganizationId AND IsDeleted = 0)
    BEGIN
        RAISERROR('DEPARTMENT_ORGANIZATION_NOT_FOUND', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.Departments (DepartmentToken, OrganizationId, Name, NormalizedName, Code, CreatedBy)
    VALUES (@DepartmentToken, @OrganizationId, @Name, @NormalizedName, @Code, @CreatedBy);

    SELECT
        d.DepartmentId, d.DepartmentToken, d.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        d.Name, d.NormalizedName, d.Code, d.IsActive,
        d.CreatedUtc, d.CreatedBy, d.LastUpdatedUtc, d.LastUpdatedBy
    FROM dbo.Departments d
    JOIN dbo.Organizations org ON org.OrganizationId = d.OrganizationId
    WHERE d.DepartmentToken = @DepartmentToken;
END;
GO
