-- =============================================================
-- MIGRATION: Seed Currencies with the active ISO 4217 currency list
-- Date: 2026-07-10
-- =============================================================
-- One-time seed of every currently circulating ISO 4217 currency code
-- (~154). Intentionally excludes ISO 4217 "fund"/precious-metal codes
-- (e.g. XAU, XAG, XDR, USN) — those aren't real transactable pricing
-- currencies. Guarded so it is a no-op if the table already has rows.
-- =============================================================

IF NOT EXISTS (SELECT 1 FROM Currencies)
BEGIN
    INSERT INTO Currencies (Code) VALUES
        ('AED'), ('AFN'), ('ALL'), ('AMD'), ('ANG'), ('AOA'), ('ARS'), ('AUD'), ('AWG'), ('AZN'),
        ('BAM'), ('BBD'), ('BDT'), ('BGN'), ('BHD'), ('BIF'), ('BMD'), ('BND'), ('BOB'), ('BRL'),
        ('BSD'), ('BTN'), ('BWP'), ('BYN'), ('BZD'), ('CAD'), ('CDF'), ('CHF'), ('CLP'), ('CNY'),
        ('COP'), ('CRC'), ('CUP'), ('CVE'), ('CZK'), ('DJF'), ('DKK'), ('DOP'), ('DZD'), ('EGP'),
        ('ERN'), ('ETB'), ('EUR'), ('FJD'), ('FKP'), ('GBP'), ('GEL'), ('GHS'), ('GIP'), ('GMD'),
        ('GNF'), ('GTQ'), ('GYD'), ('HKD'), ('HNL'), ('HTG'), ('HUF'), ('IDR'), ('ILS'), ('INR'),
        ('IQD'), ('IRR'), ('ISK'), ('JMD'), ('JOD'), ('JPY'), ('KES'), ('KGS'), ('KHR'), ('KMF'),
        ('KPW'), ('KRW'), ('KWD'), ('KYD'), ('KZT'), ('LAK'), ('LBP'), ('LKR'), ('LRD'), ('LSL'),
        ('LYD'), ('MAD'), ('MDL'), ('MGA'), ('MKD'), ('MMK'), ('MNT'), ('MOP'), ('MRU'), ('MUR'),
        ('MVR'), ('MWK'), ('MXN'), ('MYR'), ('MZN'), ('NAD'), ('NGN'), ('NIO'), ('NOK'), ('NPR'),
        ('NZD'), ('OMR'), ('PAB'), ('PEN'), ('PGK'), ('PHP'), ('PKR'), ('PLN'), ('PYG'), ('QAR'),
        ('RON'), ('RSD'), ('RUB'), ('RWF'), ('SAR'), ('SBD'), ('SCR'), ('SDG'), ('SEK'), ('SGD'),
        ('SHP'), ('SLE'), ('SOS'), ('SRD'), ('SSP'), ('STN'), ('SYP'), ('SZL'), ('THB'), ('TJS'),
        ('TMT'), ('TND'), ('TOP'), ('TRY'), ('TTD'), ('TWD'), ('TZS'), ('UAH'), ('UGX'), ('USD'),
        ('UYU'), ('UZS'), ('VES'), ('VND'), ('VUV'), ('WST'), ('XAF'), ('XCD'), ('XOF'), ('XPF'),
        ('YER'), ('ZAR'), ('ZMW'), ('ZWL');
END
GO
