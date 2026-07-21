CREATE OR ALTER PROCEDURE sp_FamilyApprovalThreshold_GetByToken
    @FamilyApprovalThresholdToken UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.FamilyApprovalThresholdId, t.FamilyApprovalThresholdToken,
        t.OrganizationId, o.OrganizationToken, o.Name AS OrganizationName,
        t.FamilyId, f.FamilyToken, f.Code AS FamilyCode,
        t.Level, t.ThresholdAmount,
        t.ApproverUserId, u.UserToken AS ApproverUserToken, u.FirstName + ' ' + u.LastName AS ApproverName,
        t.IsActive, t.CreatedUtc, t.CreatedBy, t.LastUpdatedUtc, t.LastUpdatedBy
    FROM FamilyApprovalThresholds t
    JOIN Organizations o ON o.OrganizationId = t.OrganizationId
    JOIN Families f       ON f.FamilyId       = t.FamilyId
    JOIN Users u          ON u.UserId         = t.ApproverUserId
    WHERE t.FamilyApprovalThresholdToken = @FamilyApprovalThresholdToken;
END;
GO
