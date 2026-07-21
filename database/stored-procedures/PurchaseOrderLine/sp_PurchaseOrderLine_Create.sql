SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDERLINE - CREATE
   Called once per OrderLine inside OrderService.SubmitAsync's split
   transaction — one PurchaseOrderLine per originating OrderLine,
   snapshotting its Quantity/unit/price fields independently at split
   time (not a shared row with OrderLine, see .claude/OrdersModule.md
   for why: the downstream lifecycle — received/invoiced, once Goods
   Receipts exists — needs its own home and its own audit trail).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderLine_Create
(
    @PurchaseOrderLineToken UNIQUEIDENTIFIER,
    @PurchaseOrderId        INT,
    @OrderLineId             INT,
    @ArticleId               INT,
    @Quantity                DECIMAL(18,4),
    @PurchaseUnitId          INT,
    @PurchaseQuantity        DECIMAL(18,4),
    @ContentUnitId           INT,
    @ContentQuantity         DECIMAL(18,4) = NULL,
    @UnitPrice               DECIMAL(18,4),
    @CurrencyCode            VARCHAR(3),
    @CategoryId              INT           = NULL,
    @CategoryCode            NVARCHAR(50)  = NULL,
    @SubCategoryId           INT           = NULL,
    @SubCategoryCode         NVARCHAR(50)  = NULL,
    @Notes                   NVARCHAR(500) = NULL,
    @CreatedBy               VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PurchaseOrderLine
        (PurchaseOrderLineToken, PurchaseOrderId, OrderLineId, ArticleId, Quantity,
         PurchaseUnitId, PurchaseQuantity, ContentUnitId, ContentQuantity,
         UnitPrice, CurrencyCode, CategoryId, CategoryCode, SubCategoryId, SubCategoryCode, Notes, CreatedBy)
    VALUES
        (@PurchaseOrderLineToken, @PurchaseOrderId, @OrderLineId, @ArticleId, @Quantity,
         @PurchaseUnitId, @PurchaseQuantity, @ContentUnitId, @ContentQuantity,
         @UnitPrice, @CurrencyCode, @CategoryId, @CategoryCode, @SubCategoryId, @SubCategoryCode, @Notes, @CreatedBy);

    SELECT
        pol.PurchaseOrderLineId, pol.PurchaseOrderLineToken,
        pol.PurchaseOrderId, po.PurchaseOrderToken,
        pol.OrderLineId, ol.OrderLineToken,
        pol.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.SupplierId, s.Name AS SupplierName,
        pol.Quantity,
        pol.PurchaseUnitId, pu.Code AS PurchaseUnitCode,
        pol.PurchaseQuantity,
        pol.ContentUnitId, cu.Code AS ContentUnitCode,
        pol.ContentQuantity,
        pol.UnitPrice, pol.CurrencyCode,
        pol.CategoryId, pol.CategoryCode, pol.SubCategoryId, pol.SubCategoryCode,
        pol.Notes,
        pol.CreatedUtc, pol.CreatedBy, pol.LastUpdatedUtc, pol.LastUpdatedBy
    FROM dbo.PurchaseOrderLine pol
    JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = pol.PurchaseOrderId
    JOIN dbo.OrderLine ol      ON ol.OrderLineId      = pol.OrderLineId
    JOIN dbo.Articles a        ON a.ArticleId         = pol.ArticleId
    JOIN dbo.Suppliers s       ON s.SupplierId        = a.SupplierId
    JOIN dbo.UnitsOfMeasure pu ON pu.UnitOfMeasureId  = pol.PurchaseUnitId
    JOIN dbo.UnitsOfMeasure cu ON cu.UnitOfMeasureId  = pol.ContentUnitId
    WHERE pol.PurchaseOrderLineToken = @PurchaseOrderLineToken;
END;
GO
