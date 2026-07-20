/* =============================================================
   ORGANIZATIONTYPE - GET BY ID
   Internal-only lookup (no C# interface method — called via raw
   Dapper from OrganizationService.CreateOrganizationAsync only when a
   caller explicitly overrides @OrganizationTypeId; the common case
   (omitted) is derived in C# without a DB round trip).
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_OrganizationType_GetById
    @OrganizationTypeId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT OrganizationTypeId, Code, IsActive
    FROM OrganizationTypes
    WHERE OrganizationTypeId = @OrganizationTypeId;
END;
GO
