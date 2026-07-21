SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- OrganizationId/FamilyId/Level are the row's identity and immutable after create (same
-- convention as Zone.CountryId) — only ThresholdAmount and ApproverUserId can change.
CREATE OR ALTER PROCEDURE sp_FamilyApprovalThreshold_Update
    @FamilyApprovalThresholdToken UNIQUEIDENTIFIER,
    @ThresholdAmount              DECIMAL(18,4),
    @ApproverUserId               INT,
    @LastUpdatedBy                VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM FamilyApprovalThresholds WHERE FamilyApprovalThresholdToken = @FamilyApprovalThresholdToken)
    BEGIN
        RAISERROR('FAMILY_APPROVAL_THRESHOLD_NOT_FOUND', 16, 1);
        RETURN;
    END

    UPDATE FamilyApprovalThresholds
    SET    ThresholdAmount = @ThresholdAmount,
           ApproverUserId  = @ApproverUserId,
           LastUpdatedUtc  = SYSUTCDATETIME(),
           LastUpdatedBy   = @LastUpdatedBy
    WHERE  FamilyApprovalThresholdToken = @FamilyApprovalThresholdToken;

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
