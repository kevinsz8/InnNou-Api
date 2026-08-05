SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITIONLINE - GET BY TOKEN
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_RequisitionLine_GetByToken
(
    @RequisitionLineToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rl.RequisitionLineId, rl.RequisitionLineToken, rl.RequisitionId, r.RequisitionToken,
        rl.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.PurchaseUnitId, u.Code AS PurchaseUnitCode,
        rl.QuantityRequested, rl.Notes, rl.CreatedUtc, rl.CreatedBy
    FROM dbo.RequisitionLines rl
    JOIN dbo.Requisitions r ON r.RequisitionId = rl.RequisitionId
    JOIN dbo.Articles a ON a.ArticleId = rl.ArticleId
    JOIN dbo.UnitsOfMeasure u ON u.UnitOfMeasureId = a.PurchaseUnitId
    WHERE rl.RequisitionLineToken = @RequisitionLineToken;
END;
GO
