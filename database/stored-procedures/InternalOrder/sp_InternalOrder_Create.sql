SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDER - CREATE
   Header only — lines are inserted separately via sp_InternalOrderLine_Create,
   called in a loop from the same C# transaction (mirrors PurchaseOrder +
   PurchaseOrderLine's own two-SP relationship).

   InternalOrderNumber (PI-{Year}-{5-digit number}) is assigned atomically
   from InternalOrderNumberCounters, scoped per RequestingOrganizationId per
   calendar year — same UPDATE-first, INSERT-with-duplicate-key-retry shape
   as sp_PurchaseOrder_Create's own PurchaseOrderNumberCounters logic.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrder_Create
(
    @InternalOrderToken       UNIQUEIDENTIFIER,
    @RequestingOrganizationId INT,
    @SourceOrganizationId     INT,
    @DestinationWarehouseId   INT,
    @Notes                    NVARCHAR(1000) = NULL,
    @CreatedBy                VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Year INT = YEAR(SYSUTCDATETIME());
    DECLARE @NextNumber INT;

    UPDATE dbo.InternalOrderNumberCounters
        SET @NextNumber = LastNumber = LastNumber + 1
    WHERE OrganizationId = @RequestingOrganizationId AND Year = @Year;

    IF @@ROWCOUNT = 0
    BEGIN
        BEGIN TRY
            SET @NextNumber = 1;
            INSERT INTO dbo.InternalOrderNumberCounters (OrganizationId, Year, LastNumber)
            VALUES (@RequestingOrganizationId, @Year, @NextNumber);
        END TRY
        BEGIN CATCH
            IF ERROR_NUMBER() IN (2601, 2627)
            BEGIN
                UPDATE dbo.InternalOrderNumberCounters
                    SET @NextNumber = LastNumber = LastNumber + 1
                WHERE OrganizationId = @RequestingOrganizationId AND Year = @Year;
            END
            ELSE
                THROW;
        END CATCH
    END

    DECLARE @InternalOrderNumber VARCHAR(20) = 'PI-' + CAST(@Year AS VARCHAR(4)) + '-' + RIGHT('00000' + CAST(@NextNumber AS VARCHAR(10)), 5);

    INSERT INTO dbo.InternalOrders
        (InternalOrderToken, InternalOrderNumber, RequestingOrganizationId, SourceOrganizationId, DestinationWarehouseId, Notes, CreatedBy)
    VALUES
        (@InternalOrderToken, @InternalOrderNumber, @RequestingOrganizationId, @SourceOrganizationId, @DestinationWarehouseId, @Notes, @CreatedBy);

    SELECT
        io.InternalOrderId, io.InternalOrderToken, io.InternalOrderNumber,
        io.RequestingOrganizationId, reqOrg.OrganizationToken AS RequestingOrganizationToken, reqOrg.Name AS RequestingOrganizationName,
        io.SourceOrganizationId, srcOrg.OrganizationToken AS SourceOrganizationToken, srcOrg.Name AS SourceOrganizationName,
        io.DestinationWarehouseId, dw.WarehouseToken AS DestinationWarehouseToken, dw.Name AS DestinationWarehouseName,
        ios.Code AS Status,
        io.Notes,
        io.CancelledUtc, io.CancelledBy, io.CancelledReason,
        io.CreatedUtc, io.CreatedBy, io.LastUpdatedUtc, io.LastUpdatedBy
    FROM dbo.InternalOrders io
    JOIN dbo.Organizations reqOrg ON reqOrg.OrganizationId = io.RequestingOrganizationId
    JOIN dbo.Organizations srcOrg ON srcOrg.OrganizationId = io.SourceOrganizationId
    JOIN dbo.Warehouses dw ON dw.WarehouseId = io.DestinationWarehouseId
    JOIN dbo.InternalOrderStatuses ios ON ios.InternalOrderStatusId = io.InternalOrderStatusId
    WHERE io.InternalOrderToken = @InternalOrderToken;
END;
GO
