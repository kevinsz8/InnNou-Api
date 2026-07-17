SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERTEMPLATE - GET PAGED
   @RootOrganizationId = NULL is unrestricted (SuperAdmin only) — the
   service always resolves a concrete organization for every other
   caller, same convention as sp_Order_GetPaged.
   @OwnerUserId = NULL means "every owner in scope" — only a
   SuperAdmin/SuperAssociate caller is allowed to pass NULL here; every
   other caller's service layer always supplies their own resolved
   UserId, restricting the list to their own templates.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrderTemplate_GetPaged
(
    @RootOrganizationId INT           = NULL,
    @WarehouseId        INT           = NULL,
    @OwnerUserId        INT           = NULL,
    @SearchText         NVARCHAR(200) = NULL,
    @PageNumber         INT,
    @PageSize           INT
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH OrganizationHierarchy AS
    (
        SELECT o.OrganizationId
        FROM dbo.Organizations o
        WHERE o.OrganizationId = @RootOrganizationId

        UNION ALL

        SELECT o.OrganizationId
        FROM dbo.Organizations o
        INNER JOIN OrganizationHierarchy oh ON o.ParentOrganizationId = oh.OrganizationId
    )
    SELECT
        ot.OrderTemplateId, ot.OrderTemplateToken, ot.Name,
        ot.OrganizationId, org.OrganizationToken,
        ot.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.IsActive AS IsWarehouseActive,
        ot.OwnerUserId, u.UserToken AS OwnerUserToken, u.FirstName AS OwnerFirstName, u.LastName AS OwnerLastName, u.Email AS OwnerEmail,
        ot.CreatedUtc, ot.CreatedBy, ot.LastUpdatedUtc, ot.LastUpdatedBy,
        lc.LineCount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.OrderTemplate ot
    JOIN dbo.Organizations org ON org.OrganizationId = ot.OrganizationId
    JOIN dbo.Warehouses    w   ON w.WarehouseId      = ot.WarehouseId
    JOIN dbo.Users         u   ON u.UserId           = ot.OwnerUserId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.OrderTemplateLine otl WHERE otl.OrderTemplateId = ot.OrderTemplateId) lc
    WHERE
        (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = ot.OrganizationId))
        AND (@WarehouseId IS NULL OR ot.WarehouseId = @WarehouseId)
        AND (@OwnerUserId IS NULL OR ot.OwnerUserId = @OwnerUserId)
        AND (@SearchText IS NULL OR LOWER(ot.Name) LIKE '%' + LOWER(@SearchText) + '%')
    ORDER BY ot.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
