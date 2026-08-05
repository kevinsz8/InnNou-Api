SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DEPARTMENT - EXISTS BY NORMALIZED NAME
   Scoped per-organization (two organizations may each have a
   Department named "Cocina").
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Department_ExistsByNormalizedName
(
    @OrganizationId  INT,
    @NormalizedName  NVARCHAR(150),
    @ExcludeDepartmentToken UNIQUEIDENTIFIER = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CASE WHEN EXISTS
    (
        SELECT 1
        FROM dbo.Departments
        WHERE OrganizationId = @OrganizationId
          AND NormalizedName = @NormalizedName
          AND (@ExcludeDepartmentToken IS NULL OR DepartmentToken <> @ExcludeDepartmentToken)
    )
    THEN 1 ELSE 0 END;
END;
GO
