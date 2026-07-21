SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE sp_FamilyApprovalThreshold_Create
    @FamilyApprovalThresholdToken UNIQUEIDENTIFIER,
    @OrganizationId               INT,
    @FamilyId                     INT,
    @Level                        TINYINT,
    @ThresholdAmount              DECIMAL(18,4),
    @ApproverUserId               INT,
    @CreatedBy                    VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Families WHERE FamilyId = @FamilyId AND IsActive = 1)
    BEGIN
        RAISERROR('FAMILY_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM FamilyApprovalThresholds WHERE OrganizationId = @OrganizationId AND FamilyId = @FamilyId AND Level = @Level)
    BEGIN
        RAISERROR('FAMILY_APPROVAL_THRESHOLD_LEVEL_EXISTS', 16, 1);
        RETURN;
    END

    INSERT INTO FamilyApprovalThresholds (FamilyApprovalThresholdToken, OrganizationId, FamilyId, Level, ThresholdAmount, ApproverUserId, CreatedBy)
    VALUES (@FamilyApprovalThresholdToken, @OrganizationId, @FamilyId, @Level, @ThresholdAmount, @ApproverUserId, @CreatedBy);

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
