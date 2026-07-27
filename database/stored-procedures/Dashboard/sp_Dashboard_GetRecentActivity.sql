SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DASHBOARD - GET RECENT ACTIVITY
   UNION ALL across three existing domains (Order Approvals decided,
   Purchase Orders sent, Goods Receipts recorded) — no unified activity/
   audit-log table exists in this codebase (see CLAUDE.md "no domain
   events, procedural services"), so this is the read-only reporting-layer
   equivalent: pull the last N events straight from each source table.
   ActivityType is a fixed code per branch — the frontend maps it to an
   i18n string + icon, same backend-code/frontend-translates convention as
   every other Type/Status column in this codebase.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_GetRecentActivity
(
    @RootOrganizationId INT = NULL,
    @Count              INT = 10
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
    ),
    Activity AS
    (
        SELECT
            CASE sts.Code WHEN 'APPROVED' THEN 'ORDER_APPROVAL_STEP_APPROVED' ELSE 'ORDER_APPROVAL_STEP_REJECTED' END AS ActivityType,
            s.FamilyCode AS ReferenceLabel,
            usr.FirstName + ' ' + usr.LastName AS ActorName,
            s.DecidedUtc AS OccurredUtc
        FROM dbo.OrderApprovalSteps s
        JOIN dbo.[Order] ord ON ord.OrderId = s.OrderId
        JOIN dbo.OrderApprovalStepStatuses sts ON sts.OrderApprovalStepStatusId = s.OrderApprovalStepStatusId
        LEFT JOIN dbo.Users usr ON usr.UserId = s.ApproverUserId
        WHERE s.DecidedUtc IS NOT NULL
          AND sts.Code IN ('APPROVED', 'REJECTED')
          AND (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = ord.OrganizationId))

        UNION ALL

        SELECT
            'PURCHASE_ORDER_SENT' AS ActivityType,
            po.PurchaseOrderNumber AS ReferenceLabel,
            NULL AS ActorName,
            po.SentUtc AS OccurredUtc
        FROM dbo.PurchaseOrder po
        WHERE @RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = po.OrganizationId)

        UNION ALL

        SELECT
            'GOODS_RECEIPT_CREATED' AS ActivityType,
            po.PurchaseOrderNumber AS ReferenceLabel,
            NULL AS ActorName,
            gr.CreatedUtc AS OccurredUtc
        FROM dbo.GoodsReceipt gr
        JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = gr.PurchaseOrderId
        WHERE @RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = po.OrganizationId)
    )
    SELECT TOP (@Count) ActivityType, ReferenceLabel, ActorName, OccurredUtc
    FROM Activity
    ORDER BY OccurredUtc DESC;
END;
GO
