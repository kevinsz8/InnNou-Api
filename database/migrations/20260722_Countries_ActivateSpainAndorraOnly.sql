-- =============================================================
-- MIGRATION: Restrict active Countries to Spain and Andorra only
-- Date: 2026-07-22
-- =============================================================
-- Countries.IsActive already existed and sp_Country_GetAll already
-- filters by it (default @IncludeInactive = 0). All ~195 rows seeded
-- by 20260721_Countries_Seed.sql defaulted to IsActive = 1. InnNou
-- currently only operates in Spain and Andorra, so every other
-- country is deactivated here to keep the Country picker (Zones,
-- Organizations) short and relevant. Opening a new country later is
-- just flipping its IsActive flag back to 1 — no further migration
-- pattern or code change needed, since GetAll/dropdowns already
-- react to this flag.
-- =============================================================

UPDATE Countries
SET IsActive = 0
WHERE Code NOT IN ('ES', 'AD');

UPDATE Countries
SET IsActive = 1
WHERE Code IN ('ES', 'AD');
GO
