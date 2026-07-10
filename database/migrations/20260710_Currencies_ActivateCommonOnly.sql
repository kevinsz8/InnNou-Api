-- =============================================================
-- MIGRATION: Curate active Currencies down to USD/EUR/GBP
-- Date: 2026-07-10
-- =============================================================
-- Only USD, EUR, GBP are relevant for now. The rest of the seeded
-- ISO 4217 list stays in the table (rows preserved for future use)
-- but is marked inactive so it doesn't clutter the currency picker.
-- Reactivate more later with a one-line UPDATE — plain UPDATEs are
-- safe to rerun, no guard needed.
-- =============================================================

UPDATE Currencies SET IsActive = 0 WHERE Code NOT IN ('USD', 'EUR', 'GBP');
UPDATE Currencies SET IsActive = 1 WHERE Code IN ('USD', 'EUR', 'GBP');
GO
