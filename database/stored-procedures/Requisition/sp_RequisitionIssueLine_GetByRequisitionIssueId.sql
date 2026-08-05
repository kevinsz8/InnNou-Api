SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITIONISSUELINE - GET BY REQUISITION ISSUE ID
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_RequisitionIssueLine_GetByRequisitionIssueId
(
    @RequisitionIssueId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ril.RequisitionIssueLineId, ril.RequisitionIssueLineToken,
        ril.RequisitionIssueId, ri.RequisitionIssueToken,
        ril.RequisitionLineId, rl.RequisitionLineToken, rl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        a.PurchaseUnitId, u.Code AS PurchaseUnitCode,
        ril.QuantityIssued, ril.Notes, ril.CreatedUtc, ril.CreatedBy
    FROM dbo.RequisitionIssueLines ril
    JOIN dbo.RequisitionIssues ri ON ri.RequisitionIssueId = ril.RequisitionIssueId
    JOIN dbo.RequisitionLines rl  ON rl.RequisitionLineId   = ril.RequisitionLineId
    JOIN dbo.Articles a           ON a.ArticleId             = rl.ArticleId
    JOIN dbo.UnitsOfMeasure u     ON u.UnitOfMeasureId        = a.PurchaseUnitId
    WHERE ril.RequisitionIssueId = @RequisitionIssueId
    ORDER BY ril.RequisitionIssueLineId;
END;
GO
