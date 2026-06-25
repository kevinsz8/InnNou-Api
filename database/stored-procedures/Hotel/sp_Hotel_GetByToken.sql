/* =============================================================
   HOTEL - GET BY TOKEN
   Returns a single hotel by its token. When @RootHotelId is
   provided, enforces hierarchy scope (non-admin access). Pass
   NULL to bypass the scope check (used by edit/delete).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Hotel_GetByToken
(
    @HotelToken  UNIQUEIDENTIFIER,
    @RootHotelId INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH HotelHierarchy AS
    (
        SELECT h.HotelId
        FROM dbo.Hotels h
        WHERE h.HotelId = @RootHotelId

        UNION ALL

        SELECT h.HotelId
        FROM dbo.Hotels h
        INNER JOIN HotelHierarchy hh ON h.ParentHotelId = hh.HotelId
    )
    SELECT
        h.HotelId,
        h.HotelToken,
        h.Name,
        h.NormalizedName,
        h.LegalName,
        h.Code,
        h.ParentHotelId,
        h.TimeZone,
        h.CurrencyCode,
        h.LanguageCode,
        h.IsActive,
        h.IsDeleted,
        h.CreatedUtc,
        h.CreatedBy,
        h.LastUpdatedUtc,
        h.LastUpdatedBy,
        h.DeletedUtc,
        h.DeletedBy
    FROM dbo.Hotels h
    WHERE h.HotelToken = @HotelToken
      AND h.IsDeleted = 0
      AND
      (
          @RootHotelId IS NULL
          OR EXISTS
          (
              SELECT 1 FROM HotelHierarchy hh WHERE hh.HotelId = h.HotelId
          )
      );
END;
GO
