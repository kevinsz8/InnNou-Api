SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   GOODSRECEIPT - GET PAGED
   Same visibility shape as sp_PurchaseOrder_GetPaged — @SupplierId set scopes
   to the owning supplier's own receipts; otherwise the organization-hierarchy
   branch applies (@RootOrganizationId = NULL is unrestricted, SuperAdmin only).
   @PurchaseOrderId narrows to a single PurchaseOrder's receipt history — the
   normal caller shape from the PurchaseOrder card's "Receive" modal.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_GoodsReceipt_GetPaged
(
    @RootOrganizationId INT = NULL,
    @SupplierId         INT = NULL,
    @PurchaseOrderId    INT = NULL,
    @PageNumber         INT,
    @PageSize           INT
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
    )
    SELECT
        gr.GoodsReceiptId, gr.GoodsReceiptToken,
        gr.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber,
        po.SupplierId, po.OrganizationId,
        gr.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        gr.Notes, gr.CreatedUtc, gr.CreatedBy,
        lc.LineCount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.GoodsReceipt gr
    JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = gr.PurchaseOrderId
    JOIN dbo.Warehouses w     ON w.WarehouseId      = gr.WarehouseId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.GoodsReceiptLine grl WHERE grl.GoodsReceiptId = gr.GoodsReceiptId) lc
    WHERE
        (
            (@SupplierId IS NOT NULL AND po.SupplierId = @SupplierId)
            OR (@SupplierId IS NULL AND (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = po.OrganizationId)))
        )
        AND (@PurchaseOrderId IS NULL OR gr.PurchaseOrderId = @PurchaseOrderId)
    ORDER BY gr.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
