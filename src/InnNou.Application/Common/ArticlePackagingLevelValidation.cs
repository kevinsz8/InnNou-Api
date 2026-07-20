using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Domain.Dtos;

namespace InnNou.Application.Common
{
    // Shared by CreateArticleCommandHandler, EditArticleCommandHandler, and
    // SupersedeArticleCommandHandler. Edit also calls ResolveAsync — it needs the submitted
    // chain resolved (tokens → ids) to run AreEqual against the existing article's chain and
    // detect an attempted structural change; the frontend locks the Purchase Unit/Packaging
    // Levels fields in its Edit form specifically so a real user never reaches this path with
    // an actually-changed chain, but the backend still validates and rejects it either way.
    public static class ArticlePackagingLevelValidation
    {
        public sealed class ValidationError
        {
            public required string Code { get; init; }
            public required string Description { get; init; }
            public int StatusCode { get; init; } = 400;
        }

        public sealed class Result
        {
            public List<ArticlePackagingLevelDto> Levels { get; init; } = [];
            public ValidationError? Error { get; init; }
        }

        public static async Task<Result> ResolveAsync(
            List<ArticlePackagingLevelRequest>? levels,
            IUnitOfMeasureService unitOfMeasureService,
            CancellationToken cancellationToken)
        {
            if (levels is null || levels.Count == 0)
                return new Result { Error = new ValidationError { Code = ErrorCodes.ArticlePackagingLevelsRequired, Description = "At least one packaging level (the Unidad Definida) is required." } };

            var ordered = levels.OrderBy(l => l.SequenceOrder).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].SequenceOrder != i + 1)
                    return new Result { Error = new ValidationError { Code = ErrorCodes.ArticlePackagingLevelInvalidSequence, Description = "PackagingLevels' SequenceOrder must be contiguous starting at 1, with no gaps or duplicates." } };
            }

            var definedLevels = ordered.Where(l => l.IsDefinedUnit).ToList();
            if (definedLevels.Count != 1)
                return new Result { Error = new ValidationError { Code = ErrorCodes.ArticlePackagingLevelDefinedUnitRequired, Description = "Exactly one packaging level must be marked as the Unidad Definida." } };

            if (definedLevels[0].SequenceOrder != ordered.Count)
                return new Result { Error = new ValidationError { Code = ErrorCodes.ArticlePackagingLevelDefinedUnitMustBeLast, Description = "The Unidad Definida must be the last (innermost) packaging level — no level may come after it." } };

            var resolved = new List<ArticlePackagingLevelDto>();
            foreach (var level in ordered)
            {
                if (level.QuantityInParentUnit <= 0)
                    return new Result { Error = new ValidationError { Code = ErrorCodes.ArticlePackagingLevelInvalidQuantity, Description = $"Packaging level {level.SequenceOrder}'s quantity must be a positive number." } };

                var unit = await unitOfMeasureService.GetByTokenAsync(level.UnitOfMeasureToken, cancellationToken);
                if (unit is null)
                    return new Result { Error = new ValidationError { Code = ErrorCodes.ArticlePackagingLevelUnitNotFound, Description = $"Packaging level {level.SequenceOrder}'s unit of measure was not found.", StatusCode = 404 } };

                // Every level except the Unidad Definida is a container/count level — it only
                // ever says "how many of the next level are inside", never a fixed physical
                // quantity on its own, so it must be a COUNT unit (Caja, Botella, Pallet). The
                // Unidad Definida itself can be ANY UnitType (COUNT included, e.g. "Unidad" for
                // an article with no further physical breakdown) — this is deliberately per-row
                // and per-article, not a flag fixed on UnitsOfMeasure/UnitTypes globally; see
                // CLAUDE.md's "Article packaging levels" section for the research behind this.
                if (!level.IsDefinedUnit && !string.Equals(unit.UnitTypeCode, UnitTypeCodes.Count, StringComparison.OrdinalIgnoreCase))
                    return new Result { Error = new ValidationError { Code = ErrorCodes.ArticlePackagingLevelIndefiniteUnitMustBeCount, Description = $"Packaging level {level.SequenceOrder} is not the Unidad Definida, so its unit must be a COUNT unit (e.g. BOX, BOTTLE, PALLET)." } };

                resolved.Add(new ArticlePackagingLevelDto
                {
                    SequenceOrder = level.SequenceOrder,
                    UnitOfMeasureId = unit.UnitOfMeasureId,
                    QuantityInParentUnit = level.QuantityInParentUnit,
                    IsDefinedUnit = level.IsDefinedUnit
                });
            }

            return new Result { Levels = resolved };
        }

        // Structural equality — used by Edit (must stay unchanged) and Supersede (must differ
        // from the existing article's chain, otherwise there's nothing to supersede).
        public static bool AreEqual(List<ArticlePackagingLevelDto> a, List<ArticlePackagingLevelDto> b)
        {
            if (a.Count != b.Count)
                return false;

            var orderedA = a.OrderBy(l => l.SequenceOrder).ToList();
            var orderedB = b.OrderBy(l => l.SequenceOrder).ToList();

            for (var i = 0; i < orderedA.Count; i++)
            {
                if (orderedA[i].SequenceOrder != orderedB[i].SequenceOrder ||
                    orderedA[i].UnitOfMeasureId != orderedB[i].UnitOfMeasureId ||
                    orderedA[i].QuantityInParentUnit != orderedB[i].QuantityInParentUnit ||
                    orderedA[i].IsDefinedUnit != orderedB[i].IsDefinedUnit)
                    return false;
            }

            return true;
        }
    }
}
