CREATE OR ALTER PROCEDURE sp_MenuItem_GetVisibleForContext
    @RoleLevel      INT,
    @OrganizationId INT = NULL,
    @SupplierId     INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RoleId INT;
    SELECT @RoleId = RoleId FROM Roles WHERE RoleLevel = @RoleLevel;

    -- A MenuItem with no MenuAssignments rows is visible to everyone. Once it
    -- has at least one row, it's visible only if a row matches the caller's
    -- context (NULL on a dimension = wildcard for that dimension) with
    -- IsAllowed = 1. Note: restricting a parent (group header) item does not
    -- automatically restrict its children — keep parent/child assignments
    -- consistent when tightening visibility.
    SELECT
        m.MenuItemId, m.MenuItemToken, m.ParentMenuItemId,
        m.Name, m.Route, m.Icon, m.SortOrder
    FROM MenuItems m
    WHERE m.IsActive = 1
      AND (
            NOT EXISTS (SELECT 1 FROM MenuAssignments a WHERE a.MenuItemId = m.MenuItemId)
            OR EXISTS (
                SELECT 1
                FROM MenuAssignments a
                WHERE a.MenuItemId = m.MenuItemId
                  AND a.IsAllowed = 1
                  AND (a.RoleId IS NULL OR a.RoleId = @RoleId)
                  AND (a.OrganizationId IS NULL OR a.OrganizationId = @OrganizationId)
                  AND (a.SupplierId IS NULL OR a.SupplierId = @SupplierId)
            )
          )
    ORDER BY m.ParentMenuItemId, m.SortOrder;
END;
