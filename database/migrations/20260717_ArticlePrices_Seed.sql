-- =============================================================
-- MIGRATION: Seed ArticlePrices for PRODUCT-type supplier articles
-- Date: 2026-07-17
-- =============================================================
-- Realistic EUR price data so the upcoming Orders module has something
-- to resolve a current price against. Scope: the 21 Articles belonging
-- to the 5 PRODUCT-type Suppliers only (Iberian Food Distribution,
-- AquaPura Beverages, CleanTex Hospitality Supplies, Confort Textil
-- Hotelero, Office & Print Solutions) — SERVICE/MIXED suppliers are
-- deliberately excluded, see TODO.md's note on why (fixed-unit pricing
-- doesn't fit variable-priced services yet).
--
-- Shape: one global price (OrganizationId = NULL) per article, plus a
-- handful of organization-specific contract prices on child hotels to
-- exercise the "contract wins over global" resolution path. Contract
-- rows are deliberately placed on child organizations, not their root
-- — ArticlePrices resolution matches OrganizationId exactly, it does
-- not walk the hierarchy the way currency resolution does, so a
-- contract price set on a root would never be seen by its children.
--
-- CreatedBy matches the SuperAdmin account (admin@innnou.com) that
-- seeded Suppliers/Articles/Organizations. Guarded so it is a no-op if
-- ArticlePrices already has rows.
--
-- Note: sp_ArticlePrice_Create's @ArticlePriceToken can't be passed
-- NEWID() directly as a named EXEC parameter (SQL Server rejects a
-- function-call expression there) — a variable is reassigned before
-- every call instead.
-- =============================================================

IF NOT EXISTS (SELECT 1 FROM ArticlePrices)
BEGIN
    DECLARE @CreatedBy VARCHAR(150) = '8965f941-b98d-44cc-aeaa-192c324ba086';
    DECLARE @GlobalDate DATE = '2026-07-01';
    DECLARE @ContractDate DATE = '2026-07-10';
    DECLARE @Token UNIQUEIDENTIFIER;

    -- Iberian Food Distribution (SupplierId 8)
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 14, @OrganizationId = NULL, @Price = 22.50, @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Aceite de Oliva Virgen Extra 5L
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 15, @OrganizationId = NULL, @Price = 18.90, @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Arroz Bomba 1kg Caja 10
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 16, @OrganizationId = NULL, @Price = 14.75, @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Harina de Trigo 25kg
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 17, @OrganizationId = NULL, @Price = 45.00, @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Jamón Ibérico Loncheado 1kg
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 18, @OrganizationId = NULL, @Price = 16.80, @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Tomate Triturado Lata 800g Caja 12

    -- AquaPura Beverages (SupplierId 9)
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 19, @OrganizationId = NULL, @Price = 4.80,  @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Agua Mineral 500ml Caja 24
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 20, @OrganizationId = NULL, @Price = 21.60, @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Zumo de Naranja 1L Caja 12
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 21, @OrganizationId = NULL, @Price = 33.60, @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Cerveza Artesanal 330ml Caja 24
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 22, @OrganizationId = NULL, @Price = 16.20, @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Café en Grano 1kg

    -- CleanTex Hospitality Supplies (SupplierId 10)
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 23, @OrganizationId = NULL, @Price = 9.90,  @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Detergente Multiusos 5L
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 24, @OrganizationId = NULL, @Price = 6.50,  @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Lejía Concentrada 5L
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 44, @OrganizationId = NULL, @Price = 7.25,  @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Guantes de Nitrilo Caja 100
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 45, @OrganizationId = NULL, @Price = 11.40, @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Bolsas de Basura 100L Rollo 25

    -- Confort Textil Hotelero (SupplierId 11)
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 46, @OrganizationId = NULL, @Price = 68.00, @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Juego de Sábanas Queen 300 Hilos
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 47, @OrganizationId = NULL, @Price = 12.50, @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Toalla de Baño 70x140 Blanca
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 48, @OrganizationId = NULL, @Price = 34.90, @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Albornoz Spa Talla M
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 49, @OrganizationId = NULL, @Price = 9.80,  @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Funda de Almohada Percal

    -- Office & Print Solutions (SupplierId 12)
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 50, @OrganizationId = NULL, @Price = 19.95, @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Papel A4 80g Caja 5 Resmas
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 51, @OrganizationId = NULL, @Price = 8.40,  @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Bolígrafos Caja 50
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 52, @OrganizationId = NULL, @Price = 62.00, @CurrencyCode = 'EUR', @EffectiveDate = @GlobalDate, @CreatedBy = @CreatedBy; -- Tóner Impresora Láser Negro

    -- Contract (organization-specific) prices — placed on child organizations,
    -- see note above on why not on their root.
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 14, @OrganizationId = 14, @Price = 20.00, @CurrencyCode = 'EUR', @EffectiveDate = @ContractDate, @CreatedBy = @CreatedBy; -- Aceite de Oliva → Costa Dorada Palace Marbella
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 23, @OrganizationId = 14, @Price = 8.90,  @CurrencyCode = 'EUR', @EffectiveDate = @ContractDate, @CreatedBy = @CreatedBy; -- Detergente Multiusos → Costa Dorada Palace Marbella
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 19, @OrganizationId = 10, @Price = 4.20,  @CurrencyCode = 'EUR', @EffectiveDate = @ContractDate, @CreatedBy = @CreatedBy; -- Agua Mineral → Vértice Madrid Gran Vía
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 22, @OrganizationId = 10, @Price = 14.50, @CurrencyCode = 'EUR', @EffectiveDate = @ContractDate, @CreatedBy = @CreatedBy; -- Café en Grano → Vértice Madrid Gran Vía
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 20, @OrganizationId = 22, @Price = 19.50, @CurrencyCode = 'EUR', @EffectiveDate = @ContractDate, @CreatedBy = @CreatedBy; -- Zumo de Naranja → Meridian Bilbao
    SET @Token = NEWID(); EXEC sp_ArticlePrice_Create @ArticlePriceToken = @Token, @ArticleId = 47, @OrganizationId = 18, @Price = 11.00, @CurrencyCode = 'EUR', @EffectiveDate = @ContractDate, @CreatedBy = @CreatedBy; -- Toalla de Baño → Andalucía Boutique Granada
END
