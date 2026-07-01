SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION CONTACT - SOFT DELETE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrganizationContact_SoftDelete
(
    @OrganizationContactToken UNIQUEIDENTIFIER,
    @DeletedUtc               DATETIME2,
    @DeletedBy                VARCHAR(150) = NULL,
    @LastUpdatedUtc           DATETIME2,
    @LastUpdatedBy            VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.OrganizationContacts
    SET
        IsActive       = 0,
        IsDeleted      = 1,
        DeletedUtc     = @DeletedUtc,
        DeletedBy      = @DeletedBy,
        LastUpdatedUtc = @LastUpdatedUtc,
        LastUpdatedBy  = @LastUpdatedBy
    WHERE OrganizationContactToken = @OrganizationContactToken
      AND IsDeleted = 0;
END;
GO
