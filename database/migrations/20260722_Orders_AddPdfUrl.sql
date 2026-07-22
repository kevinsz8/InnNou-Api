-- Adds [Order].PdfUrl — a nullable relative URL pointing at the order-confirmation PDF saved to
-- local disk (never a binary column; see CLAUDE.md's "Order confirmation" note). Idempotent/rerunnable.

IF COL_LENGTH('dbo.[Order]', 'PdfUrl') IS NULL
    ALTER TABLE [Order] ADD PdfUrl NVARCHAR(500) NULL;
GO
