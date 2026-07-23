SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDERLINERECTIFICATION - CREATE
   Append-only — never updated. Previous* values are resolved by the
   caller from the CURRENT EFFECTIVE state (sp_PurchaseOrderLine_GetEffective)
   before this call, not re-derived here, so the audit trail reads
   correctly even when this is the Nth rectification against the same
   PurchaseOrderLine.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderLineRectification_Create
(
    @PurchaseOrderLineRectificationToken UNIQUEIDENTIFIER,
    @PurchaseOrderRectificationId        INT,
    @PurchaseOrderLineId                 INT,
    @Action                              VARCHAR(30),
    @PreviousQuantity                    DECIMAL(18,4) = NULL,
    @NewQuantity                         DECIMAL(18,4) = NULL,
    @PreviousUnitPrice                   DECIMAL(18,4) = NULL,
    @NewUnitPrice                        DECIMAL(18,4) = NULL,
    @PreviousCurrencyCode                VARCHAR(3)    = NULL,
    @NewCurrencyCode                     VARCHAR(3)    = NULL,
    @CreatedBy                           VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PurchaseOrderLineRectifications
        (PurchaseOrderLineRectificationToken, PurchaseOrderRectificationId, PurchaseOrderLineId, PurchaseOrderRectificationLineActionId,
         PreviousQuantity, NewQuantity, PreviousUnitPrice, NewUnitPrice, PreviousCurrencyCode, NewCurrencyCode, CreatedBy)
    VALUES
        (@PurchaseOrderLineRectificationToken, @PurchaseOrderRectificationId, @PurchaseOrderLineId,
         (SELECT PurchaseOrderRectificationLineActionId FROM dbo.PurchaseOrderRectificationLineActions WHERE Code = @Action),
         @PreviousQuantity, @NewQuantity, @PreviousUnitPrice, @NewUnitPrice, @PreviousCurrencyCode, @NewCurrencyCode, @CreatedBy);

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
    WHERE lr.PurchaseOrderLineRectificationToken = @PurchaseOrderLineRectificationToken;
END;
GO
