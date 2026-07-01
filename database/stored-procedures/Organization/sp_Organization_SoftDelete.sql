SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION - SOFT DELETE
   Marks an organization as deleted and inactive, recording the
   full deleted audit trail. Does not physically remove the row.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Organization_SoftDelete
(
    @OrganizationToken UNIQUEIDENTIFIER,
    @DeletedUtc        DATETIME2(7),
    @DeletedBy         VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Organizations
    SET
        IsDeleted      = 1,
        IsActive       = 0,
        LastUpdatedUtc = @DeletedUtc,
        LastUpdatedBy  = @DeletedBy,
        DeletedUtc     = @DeletedUtc,
        DeletedBy      = @DeletedBy
    WHERE OrganizationToken = @OrganizationToken;
END;
GO
