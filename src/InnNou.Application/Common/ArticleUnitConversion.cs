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
    }
}
