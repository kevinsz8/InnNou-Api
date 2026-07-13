-- =============================================================
-- MIGRATION: Create IdempotencyKeys table
-- Date: 2026-07-13
-- =============================================================
-- Backs the new IdempotencyBehavior MediatR pipeline behavior — a
-- client-supplied "Idempotency-Key" header on a Command request gets
-- a durable record here so a retried/duplicated request returns the
-- original response instead of re-running the mutation. Scoped to
-- (Key, RequestType, UserToken) so one user's key never collides
-- with another's, and one command type's key never collides with
-- another command type reusing the same key string.
-- Guarded so it is a no-op if already applied.
-- =============================================================

IF OBJECT_ID('IdempotencyKeys', 'U') IS NULL
BEGIN
    CREATE TABLE IdempotencyKeys
    (
        IdempotencyKeyId   BIGINT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Key]              VARCHAR(255)     NOT NULL,
        RequestType        VARCHAR(300)     NOT NULL,   -- typeof(TRequest).Name
        UserToken          UNIQUEIDENTIFIER NOT NULL,   -- ActorUserToken
        RequestHash        CHAR(64)         NOT NULL,   -- SHA-256 hex of the serialized request body
        Status             VARCHAR(20)      NOT NULL,   -- 'Pending' | 'Completed'
        ResponseStatusCode INT              NULL,
        ResponseBody       NVARCHAR(MAX)    NULL,
        CreatedUtc         DATETIME2(7)     NOT NULL,
        CompletedUtc       DATETIME2(7)     NULL,
        ExpiresUtc         DATETIME2(7)     NOT NULL
    );

    CREATE UNIQUE INDEX UX_IdempotencyKeys_Key ON IdempotencyKeys ([Key], RequestType, UserToken);
    CREATE INDEX IX_IdempotencyKeys_ExpiresUtc ON IdempotencyKeys (ExpiresUtc);
END
GO
