SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   WAREHOUSE CONTACT - CREATE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_WarehouseContact_Create
(
    @WarehouseContactToken UNIQUEIDENTIFIER,
    @WarehouseId           INT,
    @ContactName            VARCHAR(150),
    @ContactType            VARCHAR(100) = NULL,
    @Department             VARCHAR(100) = NULL,
    @Phone                  VARCHAR(50)  = NULL,
    @Mobile                 VARCHAR(50)  = NULL,
    @Fax                    VARCHAR(50)  = NULL,
    @Email                  VARCHAR(320) = NULL,
    @Notes                  VARCHAR(500) = NULL,
    @IsPrimary              BIT,
    @HasAccessToSystem      BIT,
    @CreatedUtc             DATETIME2,
    @CreatedBy              VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.WarehouseContacts
    (
        WarehouseContactToken, WarehouseId, ContactName, ContactType, Department,
        Phone, Mobile, Fax, Email, Notes, IsPrimary, HasAccessToSystem,
        IsActive, IsDeleted, CreatedUtc, CreatedBy
    )
    VALUES
    (
        @WarehouseContactToken, @WarehouseId, @ContactName, @ContactType, @Department,
        @Phone, @Mobile, @Fax, @Email, @Notes, @IsPrimary, @HasAccessToSystem,
        1, 0, @CreatedUtc, @CreatedBy
    );

    SELECT
        WarehouseContactId, WarehouseContactToken, WarehouseId, ContactName, ContactType, Department,
        Phone, Mobile, Fax, Email, Notes, IsPrimary, HasAccessToSystem,
        IsActive, IsDeleted, CreatedUtc, CreatedBy, LastUpdatedUtc, LastUpdatedBy
    FROM dbo.WarehouseContacts
    WHERE WarehouseContactToken = @WarehouseContactToken;
END;
GO
