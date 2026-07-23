SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDER - CREATE
   Called once per distinct Supplier inside OrderService.SubmitAsync's
   split transaction. V1 has no approval gate — creation directly is
   sending, so it starts in SENT.

   PurchaseOrderNumber (PO-{Year}-{5-digit number}) is assigned here,
   atomically, from PurchaseOrderNumberCounters — scoped per
   Organization per calendar year (see
   20260723_PurchaseOrders_AddSequentialNumber.sql). The UPDATE-first,
   INSERT-with-duplicate-key-retry shape is the same concurrency-safe
   pattern sp_IdempotencyKey_TryBegin already uses for its own
   unique-index race (error 2601/2627).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_Create
(
    @PurchaseOrderToken UNIQUEIDENTIFIER,
    @OrderId            INT,
    @SupplierId         INT,
    @OrganizationId     INT,
    @WarehouseId        INT,
    @CreatedBy          VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Year INT = YEAR(SYSUTCDATETIME());
    DECLARE @NextNumber INT;

    UPDATE dbo.PurchaseOrderNumberCounters
        SET @NextNumber = LastNumber = LastNumber + 1
    WHERE OrganizationId = @OrganizationId AND Year = @Year;

    IF @@ROWCOUNT = 0
    BEGIN
        BEGIN TRY
            SET @NextNumber = 1;
            INSERT INTO dbo.PurchaseOrderNumberCounters (OrganizationId, Year, LastNumber)
            VALUES (@OrganizationId, @Year, @NextNumber);
        END TRY
        BEGIN CATCH
            IF ERROR_NUMBER() IN (2601, 2627)
            BEGIN
                -- Another concurrent Submit already inserted this (Organization, Year)'s
                -- counter row between our UPDATE and INSERT attempts — retry the increment.
                UPDATE dbo.PurchaseOrderNumberCounters
                    SET @NextNumber = LastNumber = LastNumber + 1
                WHERE OrganizationId = @OrganizationId AND Year = @Year;
            END
            ELSE
                THROW;
        END CATCH
    END

    DECLARE @PurchaseOrderNumber VARCHAR(20) = 'PO-' + CAST(@Year AS VARCHAR(4)) + '-' + RIGHT('00000' + CAST(@NextNumber AS VARCHAR(10)), 5);

    INSERT INTO dbo.PurchaseOrder
        (PurchaseOrderToken, PurchaseOrderNumber, OrderId, SupplierId, OrganizationId, WarehouseId, PurchaseOrderStatusId, CreatedBy)
    VALUES
        (@PurchaseOrderToken, @PurchaseOrderNumber, @OrderId, @SupplierId, @OrganizationId, @WarehouseId, (SELECT PurchaseOrderStatusId FROM dbo.PurchaseOrderStatuses WHERE Code = 'SENT'), @CreatedBy);

    SELECT
        po.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber,
        po.OrderId, ord.OrderToken,
        po.SupplierId, s.Name AS SupplierName, s.Email AS SupplierEmail, s.LanguageCode AS SupplierLanguageCode,
        po.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        po.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        pos.Code AS Status, po.SentUtc, po.CancelledUtc, po.CancelledBy,
        po.CreatedUtc, po.CreatedBy
    FROM dbo.PurchaseOrder po
    JOIN dbo.[Order] ord              ON ord.OrderId        = po.OrderId
    JOIN dbo.Suppliers s              ON s.SupplierId       = po.SupplierId
    JOIN dbo.Organizations org        ON org.OrganizationId = po.OrganizationId
    JOIN dbo.Warehouses w             ON w.WarehouseId      = po.WarehouseId
    JOIN dbo.PurchaseOrderStatuses pos ON pos.PurchaseOrderStatusId = po.PurchaseOrderStatusId
    WHERE po.PurchaseOrderToken = @PurchaseOrderToken;
END;
GO
