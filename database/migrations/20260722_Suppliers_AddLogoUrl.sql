-- Adds Suppliers.LogoUrl — a nullable relative URL pointing at a logo image saved to local
-- disk (never a binary column; see CLAUDE.md's "Supplier logo" note). Idempotent/rerunnable.

IF COL_LENGTH('dbo.Suppliers', 'LogoUrl') IS NULL
    ALTER TABLE Suppliers ADD LogoUrl NVARCHAR(500) NULL;
GO
