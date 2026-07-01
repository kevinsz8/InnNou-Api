SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION CONTACT - CREATE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrganizationContact_Create
(
    @OrganizationContactToken UNIQUEIDENTIFIER,
    @OrganizationId           INT,
    @ContactName              VARCHAR(150),
    @ContactType              VARCHAR(100) = NULL,
    @Department               VARCHAR(100) = NULL,
    @Phone                    VARCHAR(50)  = NULL,
    @Mobile                   VARCHAR(50)  = NULL,
    @Fax                      VARCHAR(50)  = NULL,
    @Email                    VARCHAR(320) = NULL,
    @Notes                    VARCHAR(500) = NULL,
    @IsPrimary                BIT,
    @CreatedUtc               DATETIME2,
    @CreatedBy                VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.OrganizationContacts
    (
        OrganizationContactToken, OrganizationId, ContactName, ContactType, Department,
        Phone, Mobile, Fax, Email, Notes, IsPrimary,
        IsActive, IsDeleted, CreatedUtc, CreatedBy
    )
    VALUES
    (
        @OrganizationContactToken, @OrganizationId, @ContactName, @ContactType, @Department,
        @Phone, @Mobile, @Fax, @Email, @Notes, @IsPrimary,
        1, 0, @CreatedUtc, @CreatedBy
    );

    SELECT
        OrganizationContactId,
        OrganizationContactToken,
        OrganizationId,
        ContactName,
        ContactType,
        Department,
        Phone,
        Mobile,
        Fax,
        Email,
        Notes,
        IsPrimary,
        IsActive,
        IsDeleted,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy
    FROM dbo.OrganizationContacts
    WHERE OrganizationContactToken = @OrganizationContactToken;
END;
GO
