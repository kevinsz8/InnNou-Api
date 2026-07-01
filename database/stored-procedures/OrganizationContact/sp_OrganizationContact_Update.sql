SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION CONTACT - UPDATE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrganizationContact_Update
(
    @OrganizationContactToken UNIQUEIDENTIFIER,
    @ContactName              VARCHAR(150),
    @ContactType              VARCHAR(100) = NULL,
    @Department               VARCHAR(100) = NULL,
    @Phone                    VARCHAR(50)  = NULL,
    @Mobile                   VARCHAR(50)  = NULL,
    @Fax                      VARCHAR(50)  = NULL,
    @Email                    VARCHAR(320) = NULL,
    @Notes                    VARCHAR(500) = NULL,
    @IsPrimary                BIT,
    @LastUpdatedUtc           DATETIME2,
    @LastUpdatedBy            VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.OrganizationContacts
    SET
        ContactName    = @ContactName,
        ContactType    = @ContactType,
        Department     = @Department,
        Phone          = @Phone,
        Mobile         = @Mobile,
        Fax            = @Fax,
        Email          = @Email,
        Notes          = @Notes,
        IsPrimary      = @IsPrimary,
        LastUpdatedUtc = @LastUpdatedUtc,
        LastUpdatedBy  = @LastUpdatedBy
    WHERE OrganizationContactToken = @OrganizationContactToken
      AND IsDeleted = 0;

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
