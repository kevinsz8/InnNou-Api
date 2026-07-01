/* =============================================================
   HOTEL - GET BY TOKEN
   Returns a single hotel by its token, scoped per caller (see
   sp_Hotel_GetPaged for the @RootHotelId/@ExactHotelId scope
   semantics). Pass both NULL to bypass the scope check entirely
   (used by HotelService when it re-fetches unrestricted before
   applying its own authorization for edit/delete).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Hotel_GetByToken
(
    @HotelToken   UNIQUEIDENTIFIER,
    @RootHotelId  INT = NULL,
    @ExactHotelId INT = NULL
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
          (@RootHotelId IS NULL AND @ExactHotelId IS NULL)
          OR (@RootHotelId IS NOT NULL AND EXISTS (SELECT 1 FROM HotelHierarchy hh WHERE hh.HotelId = h.HotelId))
          OR (@ExactHotelId IS NOT NULL AND h.HotelId = @ExactHotelId)
      );
END;
GO
