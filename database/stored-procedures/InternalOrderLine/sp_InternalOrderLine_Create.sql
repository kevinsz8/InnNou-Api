SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDERLINE - CREATE
   Called once per line inside InternalOrderService.CreateAsync's transaction,
   right after sp_InternalOrder_Create. Immutable once created — never
   updated afterward, same convention as PurchaseOrderLine.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrderLine_Create
(
    @InternalOrderLineToken UNIQUEIDENTIFIER,
    @InternalOrderId        INT,
    @ArticleId               INT,
    @Quantity                DECIMAL(18,8),
    @UnitPrice               DECIMAL(18,8),
    @CurrencyCode            VARCHAR(3),
    @Notes                   NVARCHAR(500) = NULL,
    @CreatedBy               VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InternalOrderLines
        (InternalOrderLineToken, InternalOrderId, ArticleId, Quantity, UnitPrice, CurrencyCode, Notes, CreatedBy)
    VALUES
        (@InternalOrderLineToken, @InternalOrderId, @ArticleId, @Quantity, @UnitPrice, @CurrencyCode, @Notes, @CreatedBy);

    SELECT
        iol.InternalOrderLineId, iol.InternalOrderLineToken,
        iol.InternalOrderId, io.InternalOrderToken,
        iol.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.SupplierSku,
        iol.Quantity, pu.Code AS PurchaseUnitCode,
        iol.UnitPrice, iol.CurrencyCode,
        iol.Notes,
        iol.CreatedUtc, iol.CreatedBy
    FROM dbo.InternalOrderLines iol
    JOIN dbo.InternalOrders io ON io.InternalOrderId = iol.InternalOrderId
    JOIN dbo.Articles a        ON a.ArticleId        = iol.ArticleId
    JOIN dbo.UnitsOfMeasure pu ON pu.UnitOfMeasureId  = a.PurchaseUnitId
    WHERE iol.InternalOrderLineToken = @InternalOrderLineToken;
END;
GO
