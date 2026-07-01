SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION CONTACT - GET BY TOKEN
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrganizationContact_GetByToken
(
    @OrganizationContactToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

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
        LastUpdatedBy,
        DeletedUtc,
        DeletedBy
    FROM dbo.OrganizationContacts
    WHERE OrganizationContactToken = @OrganizationContactToken
      AND IsDeleted = 0;
END;
GO
