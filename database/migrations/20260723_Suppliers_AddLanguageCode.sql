-- Adds Suppliers.LanguageCode — same nullable, unvalidated VARCHAR(10) shape as
-- Organizations.LanguageCode (no CHECK constraint; OrderConfirmationLocalization falls back to
-- "en" for a null/unrecognized code). Drives the "New purchase order" supplier email/PDF's
-- language, mirroring how Organizations.LanguageCode drives the buyer's own. Idempotent/rerunnable.

IF COL_LENGTH('dbo.Suppliers', 'LanguageCode') IS NULL
    ALTER TABLE Suppliers ADD LanguageCode VARCHAR(10) NULL;
GO
