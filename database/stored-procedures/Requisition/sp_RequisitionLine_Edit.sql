SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITIONLINE - EDIT
   Only the requested quantity/notes are editable — ArticleId is fixed once
   a line exists (add a new line instead of repurposing one), same rule as
   OrderLine's own EditLineAsync.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_RequisitionLine_Edit
(
    @RequisitionLineToken UNIQUEIDENTIFIER,
    @QuantityRequested      DECIMAL(18,8),
    @Notes                   NVARCHAR(500) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.RequisitionLines
    SET QuantityRequested = @QuantityRequested,
        Notes = @Notes
    WHERE RequisitionLineToken = @RequisitionLineToken;

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
