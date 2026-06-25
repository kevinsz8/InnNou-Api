/* =============================================================
   HOTEL - IS IN HIERARCHY
   Returns 1 if @TargetHotelId is within the subtree rooted at
   @RootHotelId, 0 otherwise. Used for authorization checks.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Hotel_IsInHierarchy
(
    @RootHotelId   INT,
    @TargetHotelId INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @TargetHotelId IS NULL
    BEGIN
        SELECT 0;
        RETURN;
    END;

    ;WITH HotelHierarchy AS
    (
        SELECT HotelId, ParentHotelId
        FROM dbo.Hotels
        WHERE HotelId = @RootHotelId
          AND IsDeleted = 0
          AND IsActive  = 1

        UNION ALL

        SELECT h.HotelId, h.ParentHotelId
        FROM dbo.Hotels h
        INNER JOIN HotelHierarchy hh ON h.ParentHotelId = hh.HotelId
        WHERE h.IsDeleted = 0
          AND h.IsActive  = 1
    )
    SELECT
        CASE
            WHEN EXISTS
            (
                SELECT 1 FROM HotelHierarchy WHERE HotelId = @TargetHotelId
            )
            THEN 1
            ELSE 0
        END;
END;
GO
