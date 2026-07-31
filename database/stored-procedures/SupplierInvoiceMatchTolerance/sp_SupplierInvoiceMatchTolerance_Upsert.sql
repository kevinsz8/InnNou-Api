SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICEMATCHTOLERANCE - UPSERT
   Creates or updates the calling organization's own tolerance row. Setting
   your own row always takes priority over an inherited one from a Super
   Asociado ancestor (see sp_SupplierInvoiceMatchTolerance_GetEffective).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoiceMatchTolerance_Upsert
(
    @OrganizationId  INT,
    @TolerancePercent DECIMAL(11,8),
    @ToleranceAmount  DECIMAL(18,8),
    @LastUpdatedBy    VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.SupplierInvoiceMatchTolerances WHERE OrganizationId = @OrganizationId)
    BEGIN
        UPDATE dbo.SupplierInvoiceMatchTolerances
        SET TolerancePercent = @TolerancePercent,
            ToleranceAmount  = @ToleranceAmount,
            LastUpdatedUtc   = SYSUTCDATETIME(),
            LastUpdatedBy    = @LastUpdatedBy
        WHERE OrganizationId = @OrganizationId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.SupplierInvoiceMatchTolerances (OrganizationId, TolerancePercent, ToleranceAmount, CreatedBy)
        VALUES (@OrganizationId, @TolerancePercent, @ToleranceAmount, @LastUpdatedBy);
    END

    SELECT t.SupplierInvoiceMatchToleranceId, t.SupplierInvoiceMatchToleranceToken,
           t.OrganizationId AS EffectiveOrganizationId, org.OrganizationToken AS EffectiveOrganizationToken,
           org.Name AS EffectiveOrganizationName,
           t.TolerancePercent, t.ToleranceAmount, CAST(0 AS BIT) AS IsInherited
    FROM dbo.SupplierInvoiceMatchTolerances t
    JOIN dbo.Organizations org ON org.OrganizationId = t.OrganizationId
    WHERE t.OrganizationId = @OrganizationId;
END;
GO
