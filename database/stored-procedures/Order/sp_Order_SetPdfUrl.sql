SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDER - SET PDF URL
   Sets or clears (NULL) the order-confirmation PDF path — the actual
   file lives on local disk (see CLAUDE.md's "Order confirmation" note),
   this only persists the relative URL used to fetch it. Order has no
   soft-delete, so unlike sp_Supplier_SetLogoUrl there is no IsDeleted
   filter.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Order_SetPdfUrl
(
    @OrderToken     UNIQUEIDENTIFIER,
    @PdfUrl         NVARCHAR(500) = NULL,
    @LastUpdatedUtc DATETIME2(7),
    @LastUpdatedBy  VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.[Order]
    SET
        PdfUrl         = @PdfUrl,
        LastUpdatedUtc = @LastUpdatedUtc,
        LastUpdatedBy  = @LastUpdatedBy
    WHERE OrderToken = @OrderToken;

    SELECT
        o.OrderId, o.OrderToken, o.OrganizationId, org.OrganizationToken,
        o.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        os.Code AS Status, o.Notes, o.SubmittedUtc, o.PdfUrl,
        o.CreatedUtc, o.CreatedBy, o.LastUpdatedUtc, o.LastUpdatedBy
    FROM dbo.[Order] o
    JOIN dbo.Organizations org ON org.OrganizationId = o.OrganizationId
    JOIN dbo.Warehouses    w   ON w.WarehouseId      = o.WarehouseId
    JOIN dbo.OrderStatuses os  ON os.OrderStatusId    = o.OrderStatusId
    WHERE o.OrderToken = @OrderToken;
END;
GO
