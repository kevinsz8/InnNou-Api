SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITIONLINE - GET BY REQUISITION ID
   QuantityIssued is the cumulative sum across every RequisitionIssueLine
   ever posted against this line (there is no rectification concept here,
   unlike PurchaseOrderLine's own GetEffective, so a plain SUM is enough —
   no need for a separate GetEffective SP).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_RequisitionLine_GetByRequisitionId
(
    @RequisitionId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rl.RequisitionLineId, rl.RequisitionLineToken, rl.RequisitionId, r.RequisitionToken,
        rl.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.PurchaseUnitId, u.Code AS PurchaseUnitCode,
        rl.QuantityRequested, ISNULL(iss.QuantityIssued, 0) AS QuantityIssued,
        rl.RequestedUnitId, ru.Code AS RequestedUnitCode, rl.RequestedQuantity,
        rl.Notes, rl.CreatedUtc, rl.CreatedBy
    FROM dbo.RequisitionLines rl
    JOIN dbo.Requisitions r ON r.RequisitionId = rl.RequisitionId
    JOIN dbo.Articles a ON a.ArticleId = rl.ArticleId
    JOIN dbo.UnitsOfMeasure u ON u.UnitOfMeasureId = a.PurchaseUnitId
    LEFT JOIN dbo.UnitsOfMeasure ru ON ru.UnitOfMeasureId = rl.RequestedUnitId
    OUTER APPLY
    (
        SELECT SUM(ril.QuantityIssued) AS QuantityIssued
        FROM dbo.RequisitionIssueLines ril
        WHERE ril.RequisitionLineId = rl.RequisitionLineId
    ) iss
    WHERE rl.RequisitionId = @RequisitionId
    ORDER BY rl.RequisitionLineId;
END;
GO
