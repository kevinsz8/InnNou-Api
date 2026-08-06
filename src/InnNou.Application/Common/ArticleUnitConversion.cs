using InnNou.Domain.Dtos;

namespace InnNou.Application.Common
{
    // Shared by every write path that lets a user enter a quantity in something other than
    // Article.PurchaseUnitId — Requisitions (RequisitionLine/RequisitionIssueLine), Inventory
    // Adjustments/Transfers (InventoryMovement), and Inventory Period Counts
    // (InventoryPeriodCount). Deliberately NOT used by Orders/PurchaseOrders/GoodsReceipts —
    // those stay denominated in PurchaseUnitId only, since that's the actual contract unit with
    // the supplier (researched against SAP/Oracle Hospitality Materials Control/Odoo before
    // building; all three keep the purchase/order unit fixed and only let *internal* stock
    // movement/consumption/count use a different unit — see .claude/RequisitionsModule.md).
    //
    // No new conversion-rate table needed: Article.PurchaseUnitId together with that article's
    // own ArticlePackagingLevels chain already encodes every factor required. Each level's
    // QuantityInParentUnit is "how many of this level's unit make up one of the level above it"
    // (SequenceOrder 1's parent is the Purchase Unit itself) — so the cumulative product of
    // QuantityInParentUnit from SequenceOrder 1 up to and including a given level is "how many
    // of that level's unit make up one Purchase Unit." Converting an entered quantity in that
    // level's unit back to Purchase Unit terms is just dividing by that cumulative product.
    public static class ArticleUnitConversion
    {
        // The units a user may enter a quantity in for this article: the Purchase Unit itself,
        // plus every level in its own packaging chain. Order matches display order — Purchase
        // Unit first (the default/most common choice), then the chain from outermost to
        // innermost (SequenceOrder ascending), deduplicated by UnitOfMeasureId.
        public static List<int> GetRequestableUnitIds(int purchaseUnitId, IReadOnlyList<ArticlePackagingLevelDto> packagingLevels)
        {
            var ids = new List<int> { purchaseUnitId };
            ids.AddRange(packagingLevels.OrderBy(l => l.SequenceOrder).Select(l => l.UnitOfMeasureId));
            return ids.Distinct().ToList();
        }

        // Converts a quantity entered in enteredUnitId to Article.PurchaseUnitId terms. Returns
        // null if enteredUnitId is neither the Purchase Unit itself nor a level in this
        // article's own packaging chain — the caller should treat that as a validation failure
        // (ArticleUnitNotValidForArticle), not silently fall back to any default.
        public static decimal? ToPurchaseUnitQuantity(
            int purchaseUnitId,
            IReadOnlyList<ArticlePackagingLevelDto> packagingLevels,
            int enteredUnitId,
            decimal enteredQuantity)
        {
            if (enteredUnitId == purchaseUnitId)
                return enteredQuantity;

            var cumulative = 1m;
            foreach (var level in packagingLevels.OrderBy(l => l.SequenceOrder))
            {
                cumulative *= level.QuantityInParentUnit;
                if (level.UnitOfMeasureId == enteredUnitId)
                    return enteredQuantity / cumulative;
            }

            return null;
        }

        // One step of a resolved chain, for reporting — see GetCumulativeChain.
        public sealed class ChainStep
        {
            public int SequenceOrder { get; set; }
            public int UnitOfMeasureId { get; set; }
            public string? UnitOfMeasureCode { get; set; }
            public Dictionary<string, string>? UnitOfMeasureNameTranslations { get; set; }
            // "How many of this level's unit make up one Purchase Unit" — the same cumulative
            // product ToPurchaseUnitQuantity divides by, just returned per-level instead of only
            // for a single target unit. Used by the packaging-conversion report so a caller can
            // see the full breakdown (e.g. "1 BOX = 24 BOTTLE = 12000 ML") without re-deriving it.
            public decimal QuantityPerPurchaseUnit { get; set; }
            public bool IsDefinedUnit { get; set; }
        }

        public static List<ChainStep> GetCumulativeChain(IReadOnlyList<ArticlePackagingLevelDto> packagingLevels)
        {
            var steps = new List<ChainStep>();
            var cumulative = 1m;
            foreach (var level in packagingLevels.OrderBy(l => l.SequenceOrder))
            {
                cumulative *= level.QuantityInParentUnit;
                steps.Add(new ChainStep
                {
                    SequenceOrder = level.SequenceOrder,
                    UnitOfMeasureId = level.UnitOfMeasureId,
                    UnitOfMeasureCode = level.UnitOfMeasureCode,
                    UnitOfMeasureNameTranslations = level.UnitOfMeasureNameTranslations,
                    QuantityPerPurchaseUnit = cumulative,
                    IsDefinedUnit = level.IsDefinedUnit
                });
            }
            return steps;
        }

        // The chain's own "Unidad Definida" — the last level (highest SequenceOrder), the one
        // real fixed-quantity unit every article always has exactly one of. Used as a universal
        // secondary reference for any quantity display: regardless of which unit a Requisition/
        // Inventory quantity was actually entered in, "how much is that in the article's most
        // atomic unit" is always a meaningful thing to show alongside it. Returns null only if
        // packagingLevels is empty, which shouldn't happen for any saved Article (every article
        // requires at least one level) but callers should handle defensively.
        public static ChainStep? GetDefinedUnitStep(IReadOnlyList<ArticlePackagingLevelDto> packagingLevels)
            => GetCumulativeChain(packagingLevels).OrderByDescending(s => s.SequenceOrder).FirstOrDefault();

        // Converts a quantity denominated in fromUnitId to toUnitId terms, where both units must
        // each be either this article's own Purchase Unit or a level in its packaging chain (the
        // same "requestable unit" set GetRequestableUnitIds returns) — returns null otherwise,
        // same validation stance as ToPurchaseUnitQuantity. Works by routing through the Purchase
        // Unit as a common denominator: every unit's own cumulative product (GetCumulativeChain)
        // already expresses "how many of this unit make up one Purchase Unit," so converting
        // between any two arbitrary units A and B is just fromQuantity / cumulativeA * cumulativeB
        // — no different from the existing purchase-unit-only conversion, just generalized to a
        // target other than the Purchase Unit itself.
        public static decimal? ConvertQuantity(
            int purchaseUnitId,
            IReadOnlyList<ArticlePackagingLevelDto> packagingLevels,
            int fromUnitId,
            decimal fromQuantity,
            int toUnitId)
        {
            if (fromUnitId == toUnitId)
                return fromQuantity;

            var cumulativeByUnit = new Dictionary<int, decimal> { [purchaseUnitId] = 1m };
            foreach (var step in GetCumulativeChain(packagingLevels))
                cumulativeByUnit[step.UnitOfMeasureId] = step.QuantityPerPurchaseUnit;

            if (!cumulativeByUnit.TryGetValue(fromUnitId, out var fromCumulative) ||
                !cumulativeByUnit.TryGetValue(toUnitId, out var toCumulative))
                return null;

            return fromQuantity / fromCumulative * toCumulative;
        }

        // A ready-to-display secondary reference for any already-resolved quantity+unit — used
        // by every read path that shows a line's own "effective" quantity (its raw entered pair
        // if one was captured, else the canonical Purchase-Unit-normalized value) and wants to
        // also surface "how much is that in the article's most atomic (Unidad Definida) unit."
        // Returns null when there's nothing useful to add — either the chain has no levels, or
        // the effective unit already IS the defined unit (showing "= the same number" would be
        // redundant, not informative).
        public static (string? Code, Dictionary<string, string>? NameTranslations, decimal Quantity)? GetDefinedUnitEquivalent(
            int purchaseUnitId,
            IReadOnlyList<ArticlePackagingLevelDto> packagingLevels,
            int effectiveUnitId,
            decimal effectiveQuantity)
        {
            var definedStep = GetDefinedUnitStep(packagingLevels);
            if (definedStep is null || definedStep.UnitOfMeasureId == effectiveUnitId)
                return null;

            var converted = ConvertQuantity(purchaseUnitId, packagingLevels, effectiveUnitId, effectiveQuantity, definedStep.UnitOfMeasureId);
            return converted is null ? null : (definedStep.UnitOfMeasureCode, definedStep.UnitOfMeasureNameTranslations, converted.Value);
        }
    }
}
