SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   WAREHOUSE - GET PAGED BY ORGANIZATION ID
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Warehouse_GetPagedByOrganizationId
(
    @OrganizationId  INT,
    @PageNumber      INT,
    @PageSize        INT,
    @SearchText      VARCHAR(200) = NULL,
    @IncludeInactive BIT          = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        w.WarehouseId, w.WarehouseToken, w.OrganizationId, w.Name, w.NormalizedName, w.Code, w.Description, w.PurposeCode,
        w.IsInventoriable, w.CanReceivePurchases, w.CanReceiveTransfers, w.CanTransferOut,
        w.CanConsumeInventory, w.CanProduceItems, w.CanSellItems, w.CanAdjustInventory, w.CanReceiveReturns,
        w.TrackLotNumbers, w.TrackExpirationDates, w.TrackSerialNumbers, w.RequireApproval,
        w.IsDefaultReceivingWarehouse, w.IsDefaultConsumptionWarehouse,
        w.IsActive, w.IsDeleted, w.CreatedUtc, w.CreatedBy, w.LastUpdatedUtc, w.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.Warehouses w
    WHERE
        w.OrganizationId = @OrganizationId
        AND w.IsDeleted = 0
        AND (@IncludeInactive = 1 OR w.IsActive = 1)
        AND
        (
            @SearchText IS NULL
            OR LOWER(w.Name)                LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(w.Code, ''))    LIKE '%' + LOWER(@SearchText) + '%'
        )
    ORDER BY w.Name
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
