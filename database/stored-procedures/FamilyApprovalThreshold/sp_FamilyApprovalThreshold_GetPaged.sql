-- Always scoped to a single organization — thresholds have no "global" browse view, unlike
-- Category. @OrganizationId is required.
CREATE OR ALTER PROCEDURE sp_FamilyApprovalThreshold_GetPaged
(
    @OrganizationId  INT,
    @PageNumber      INT,
    @PageSize        INT,
    @FamilyId        INT = NULL,
    @IncludeInactive BIT = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.FamilyApprovalThresholdId, t.FamilyApprovalThresholdToken,
        t.OrganizationId, o.OrganizationToken, o.Name AS OrganizationName,
        t.FamilyId, f.FamilyToken, f.Code AS FamilyCode,
        t.Level, t.ThresholdAmount,
        t.ApproverUserId, u.UserToken AS ApproverUserToken, u.FirstName + ' ' + u.LastName AS ApproverName,
        t.IsActive, t.CreatedUtc, t.CreatedBy, t.LastUpdatedUtc, t.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM FamilyApprovalThresholds t
    JOIN Organizations o ON o.OrganizationId = t.OrganizationId
    JOIN Families f       ON f.FamilyId       = t.FamilyId
    JOIN Users u          ON u.UserId         = t.ApproverUserId
    WHERE t.OrganizationId = @OrganizationId
      AND (@IncludeInactive = 1 OR t.IsActive = 1)
      AND (@FamilyId IS NULL OR t.FamilyId = @FamilyId)
    ORDER BY f.Code, t.Level
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
