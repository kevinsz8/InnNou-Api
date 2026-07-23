SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderLineRectification_GetByRectificationId
(
    @PurchaseOrderRectificationId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        lr.PurchaseOrderLineRectificationId, lr.PurchaseOrderLineRectificationToken,
        lr.PurchaseOrderRectificationId,
        lr.PurchaseOrderLineId, pol.PurchaseOrderLineToken,
        pol.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        actions.Code AS Action,
        lr.PreviousQuantity, lr.NewQuantity, lr.PreviousUnitPrice, lr.NewUnitPrice, lr.PreviousCurrencyCode, lr.NewCurrencyCode,
        lr.CreatedUtc, lr.CreatedBy
    FROM dbo.PurchaseOrderLineRectifications lr
    JOIN dbo.PurchaseOrderLine pol ON pol.PurchaseOrderLineId = lr.PurchaseOrderLineId
    JOIN dbo.Articles a ON a.ArticleId = pol.ArticleId
    JOIN dbo.PurchaseOrderRectificationLineActions actions ON actions.PurchaseOrderRectificationLineActionId = lr.PurchaseOrderRectificationLineActionId
    WHERE lr.PurchaseOrderRectificationId = @PurchaseOrderRectificationId
    ORDER BY lr.PurchaseOrderLineRectificationId;
END;
GO
