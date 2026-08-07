using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace InnNou.Infrastructure.Services;

public class ArticleDiscountService(
    IDbConnectionFactory connectionFactory,
    IMapper mapper,
    ICurrencyService currencyService,
    INotificationService notificationService,
    ILogger<ArticleDiscountService> logger) : IArticleDiscountService
{
    private const int AdminRoleLevel = 80;
    private const int MaxPageSize = 100;

    private sealed class ArticleDiscountPageRow : ArticleDiscount { public int TotalCount { get; set; } }

    // Projection for sp_SupplierPriceChangeSubscription_GetSubscribers — same shape
    // ArticlePriceService.NotifySupplierPriceSubscribersAsync already uses for the sibling
    // Supplier_Price_Updated notification; this reuses the identical opt-in subscriber list.
    private sealed class SupplierPriceSubscriberRow
    {
        public int UserId { get; set; }
        public Guid UserToken { get; set; }
        public int? OrganizationId { get; set; }
    }

    private sealed class ArticleDiscountScopeRow
    {
        public int ArticleDiscountId { get; set; }
        public Guid ArticleDiscountToken { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveUntil { get; set; }
        public string? Description { get; set; }
    }

    private sealed class EffectiveArticleDiscountRow
    {
        public int ArticleDiscountId { get; set; }
        public Guid ArticleDiscountToken { get; set; }
        public int DiscountTypeId { get; set; }
        public string DiscountTypeCode { get; set; } = default!;
        public decimal DiscountValue { get; set; }
        public string? CurrencyCode { get; set; }
        public string ScopeLevel { get; set; } = default!;
    }

    // Suppliers own ArticleDiscounts, same ownership shape as ArticlePriceService.CanManage —
    // only the owning supplier (real login or impersonated) or Admin+ may manage/view them.
    private static bool CanManage(IRequestContext context, int supplierId) =>
        context.SupplierId.HasValue
            ? context.SupplierId.Value == supplierId
            : context.RoleLevel >= AdminRoleLevel;

    private static void EnsureCanManage(IRequestContext context, int supplierId)
    {
        if (!CanManage(context, supplierId))
            throw new ApiException(ErrorCodes.ArticleDiscountForbidden, "Not allowed to manage discounts for this supplier.", 403);
    }

    private static bool DatesOverlap(DateTime aFrom, DateTime? aUntil, DateTime bFrom, DateTime? bUntil) =>
        aFrom <= (bUntil ?? DateTime.MaxValue) && bFrom <= (aUntil ?? DateTime.MaxValue);

    private async Task<Supplier> ResolveSupplierAsync(IDbConnection connection, Guid supplierToken)
    {
        var supplier = await connection.QueryFirstOrDefaultAsync<Supplier>(
            "sp_Supplier_GetByToken", new { SupplierToken = supplierToken }, commandType: CommandType.StoredProcedure);
        if (supplier is null)
            throw new ApiException(ErrorCodes.SupplierNotFound, "Supplier not found.", 404);
        return supplier;
    }

    // Resolves the scope (at most one of Article/SubFamily/Family) and validates the discount's
    // own terms (type/value/currency/date-range) — shared by both CreateAsync and EditAsync so
    // the two never drift on validation rules.
    private async Task<(int? ArticleId, int? SubFamilyId, int? FamilyId, int DiscountTypeId)> ResolveScopeAndValidateAsync(
        IDbConnection connection, int supplierId, Guid? articleToken, Guid? subFamilyToken, Guid? familyToken,
        string discountTypeCode, decimal discountValue, string? currencyCode, DateTime effectiveFrom, DateTime? effectiveUntil)
    {
        var scopeCount = (articleToken.HasValue ? 1 : 0) + (subFamilyToken.HasValue ? 1 : 0) + (familyToken.HasValue ? 1 : 0);
        if (scopeCount > 1)
            throw new ApiException(ErrorCodes.ArticleDiscountInvalidScope, "At most one of Article, SubFamily, or Family may be set — leave all blank for a supplier-wide discount.", 400);

        int? articleId = null;
        int? subFamilyId = null;
        int? familyId = null;

        if (articleToken.HasValue)
        {
            var article = await connection.QueryFirstOrDefaultAsync<Article>(
                "sp_Article_GetByToken", new { ArticleToken = articleToken.Value }, commandType: CommandType.StoredProcedure);
            if (article is null)
                throw new ApiException(ErrorCodes.ArticleNotFound, "Article not found.", 404);
            if (article.SupplierId != supplierId)
                throw new ApiException(ErrorCodes.ArticleDiscountArticleNotOwnedBySupplier, "This article does not belong to the discount's supplier.", 400);
            if (article.ReplacedByArticleId.HasValue)
                throw new ApiException(ErrorCodes.ArticleDiscountArticleReplaced, "This article has been superseded — discount the replacement article instead.", 400);
            articleId = article.ArticleId;
        }
        else if (subFamilyToken.HasValue)
        {
            var subFamily = await connection.QueryFirstOrDefaultAsync<SubFamily>(
                "sp_SubFamily_GetByToken", new { SubFamilyToken = subFamilyToken.Value }, commandType: CommandType.StoredProcedure);
            if (subFamily is null)
                throw new ApiException(ErrorCodes.SubFamilyNotFound, "SubFamily not found.", 404);
            subFamilyId = subFamily.SubFamilyId;
        }
        else if (familyToken.HasValue)
        {
            var family = await connection.QueryFirstOrDefaultAsync<Family>(
                "sp_Family_GetByToken", new { FamilyToken = familyToken.Value }, commandType: CommandType.StoredProcedure);
            if (family is null)
                throw new ApiException(ErrorCodes.FamilyNotFound, "Family not found.", 404);
            familyId = family.FamilyId;
        }

        if (!DiscountTypeCodes.IsValid(discountTypeCode))
            throw new ApiException(ErrorCodes.ArticleDiscountInvalidType, "Invalid discount type.", 400);
        var discountType = DiscountTypeCodes.FromCode(discountTypeCode);

        if (discountValue <= 0)
            throw new ApiException(ErrorCodes.ArticleDiscountInvalidValue, "Discount value must be greater than zero.", 400);

        if (discountType == DiscountType.Percentage)
        {
            if (discountValue > 100)
                throw new ApiException(ErrorCodes.ArticleDiscountPercentageExceedsMax, "A percentage discount cannot exceed 100.", 400);
            if (!string.IsNullOrWhiteSpace(currencyCode))
                throw new ApiException(ErrorCodes.ArticleDiscountCurrencyNotAllowed, "A percentage discount must not specify a currency.", 400);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
                throw new ApiException(ErrorCodes.ArticleDiscountCurrencyRequired, "A fixed-amount discount requires a currency.", 400);
            if (!await currencyService.ExistsActiveByCodeAsync(currencyCode, CancellationToken.None))
                throw new ApiException(ErrorCodes.ArticleDiscountInvalidCurrency, $"Currency '{currencyCode}' is not a recognized, active currency.", 400);
        }

        if (effectiveUntil.HasValue && effectiveUntil.Value.Date < effectiveFrom.Date)
            throw new ApiException(ErrorCodes.ArticleDiscountInvalidDateRange, "EffectiveUntil cannot be earlier than EffectiveFrom.", 400);

        return (articleId, subFamilyId, familyId, (int)discountType);
    }

    private async Task EnsureNoOverlapAsync(IDbConnection connection, int supplierId, int? articleId, int? subFamilyId, int? familyId,
        DateTime effectiveFrom, DateTime? effectiveUntil, Guid? excludeToken)
    {
        var siblings = (await connection.QueryAsync<ArticleDiscountScopeRow>(
            "sp_ArticleDiscount_GetByScope",
            new { SupplierId = supplierId, ArticleId = articleId, SubFamilyId = subFamilyId, FamilyId = familyId, ExcludeToken = excludeToken },
            commandType: CommandType.StoredProcedure)).ToList();

        if (siblings.Any(s => DatesOverlap(effectiveFrom, effectiveUntil, s.EffectiveFrom, s.EffectiveUntil)))
            throw new ApiException(ErrorCodes.ArticleDiscountOverlapping, "An active discount already covers this scope during an overlapping date range.", 409);
    }

    public async Task<PagedResult<ArticleDiscountDto>> GetPagedAsync(Guid supplierToken, int pageNumber, int pageSize, bool includeInactive, IRequestContext context, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();
        var supplier = await ResolveSupplierAsync(connection, supplierToken);
        EnsureCanManage(context, supplier.SupplierId);

        var p = new DynamicParameters();
        p.Add("@SupplierId", supplier.SupplierId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<ArticleDiscountPageRow>(
            "sp_ArticleDiscount_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<ArticleDiscountDto>
        {
            Items = mapper.MapList<ArticleDiscountDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<ArticleDiscountDto?> GetByTokenAsync(Guid token, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<ArticleDiscount>(
            "sp_ArticleDiscount_GetByToken", new { ArticleDiscountToken = token }, commandType: CommandType.StoredProcedure);
        if (row is null)
            return null;

        EnsureCanManage(context, row.SupplierId);
        return mapper.Map<ArticleDiscountDto>(row);
    }

    public async Task<ArticleDiscountDto?> CreateAsync(Guid supplierToken, Guid? articleToken, Guid? subFamilyToken, Guid? familyToken, string discountTypeCode, decimal discountValue, string? currencyCode, DateTime effectiveFrom, DateTime? effectiveUntil, string? description, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var supplier = await ResolveSupplierAsync(connection, supplierToken);
        EnsureCanManage(context, supplier.SupplierId);

        var (articleId, subFamilyId, familyId, discountTypeId) = await ResolveScopeAndValidateAsync(
            connection, supplier.SupplierId, articleToken, subFamilyToken, familyToken,
            discountTypeCode, discountValue, currencyCode, effectiveFrom, effectiveUntil);

        await EnsureNoOverlapAsync(connection, supplier.SupplierId, articleId, subFamilyId, familyId, effectiveFrom, effectiveUntil, excludeToken: null);

        var p = new DynamicParameters();
        p.Add("@ArticleDiscountToken", Guid.NewGuid());
        p.Add("@SupplierId", supplier.SupplierId);
        p.Add("@ArticleId", articleId);
        p.Add("@SubFamilyId", subFamilyId);
        p.Add("@FamilyId", familyId);
        p.Add("@DiscountTypeId", discountTypeId);
        p.Add("@DiscountValue", discountValue);
        p.Add("@CurrencyCode", currencyCode);
        p.Add("@EffectiveFrom", effectiveFrom.Date);
        p.Add("@EffectiveUntil", effectiveUntil?.Date);
        p.Add("@Description", description);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<ArticleDiscount>(
            "sp_ArticleDiscount_Create", p, commandType: CommandType.StoredProcedure);
        if (row is null)
            return null;

        await NotifySupplierPriceSubscribersAsync(connection, row, context, cancellationToken);

        return mapper.Map<ArticleDiscountDto>(row);
    }

    // Best-effort/non-blocking, same convention as every notification call site in this codebase —
    // a failure here must never fail an already-committed discount. Reuses the exact same opt-in
    // subscriber list ArticlePriceService.NotifySupplierPriceSubscribersAsync already fires
    // Supplier_Price_Updated to — a new discount is exactly the kind of "price just changed for a
    // moment" event those subscribers signed up to hear about (see .claude/ArticleDiscountModule.md).
    private async Task NotifySupplierPriceSubscribersAsync(DbConnection connection, ArticleDiscount discount, IRequestContext context, CancellationToken cancellationToken)
    {
        try
        {
            var subscribers = (await connection.QueryAsync<SupplierPriceSubscriberRow>(
                "sp_SupplierPriceChangeSubscription_GetSubscribers",
                new { SupplierId = discount.SupplierId },
                commandType: CommandType.StoredProcedure)).ToList();

            if (subscribers.Count == 0)
                return;

            var scopeLabel = discount.ArticleName ?? discount.SubFamilyCode ?? discount.FamilyCode ?? "all articles";
            object data = new
            {
                supplierName = discount.SupplierName,
                scopeLabel,
                discountTypeCode = discount.DiscountTypeCode,
                discountValue = discount.DiscountValue,
                currencyCode = discount.CurrencyCode,
                effectiveFrom = discount.EffectiveFrom,
                effectiveUntil = discount.EffectiveUntil
            };

            foreach (var subscriber in subscribers)
            {
                // notificationService.NotifyAsync opens its own connection — close this one first
                // (Dapper transparently reopens it on the next loop iteration's query) so at most
                // one connection is ever open at once — see ArticlePriceService's own identical
                // comment and .claude/NotificationsModule.md.
                await connection.CloseAsync();
                await notificationService.NotifyAsync(
                    subscriber.UserToken, NotificationType.Article_Discount_Created, data,
                    "/articles", context, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Article-discount-created notification failed for ArticleDiscountId {ArticleDiscountId}", discount.ArticleDiscountId);
        }
    }

    public async Task<ArticleDiscountDto?> EditAsync(Guid token, string discountTypeCode, decimal discountValue, string? currencyCode, DateTime effectiveFrom, DateTime? effectiveUntil, string? description, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<ArticleDiscount>(
            "sp_ArticleDiscount_GetByToken", new { ArticleDiscountToken = token }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            return null;

        EnsureCanManage(context, existing.SupplierId);

        // Scope (Supplier/Article/SubFamily/Family) is immutable after create — only the terms
        // are editable, same convention as FamilyApprovalThreshold's OrganizationId/FamilyId/Level.
        var (_, _, _, discountTypeId) = await ResolveScopeAndValidateAsync(
            connection, existing.SupplierId,
            existing.ArticleId.HasValue ? existing.ArticleToken : null,
            existing.SubFamilyId.HasValue ? existing.SubFamilyToken : null,
            existing.FamilyId.HasValue ? existing.FamilyToken : null,
            discountTypeCode, discountValue, currencyCode, effectiveFrom, effectiveUntil);

        await EnsureNoOverlapAsync(connection, existing.SupplierId, existing.ArticleId, existing.SubFamilyId, existing.FamilyId, effectiveFrom, effectiveUntil, excludeToken: token);

        var p = new DynamicParameters();
        p.Add("@ArticleDiscountToken", token);
        p.Add("@DiscountTypeId", discountTypeId);
        p.Add("@DiscountValue", discountValue);
        p.Add("@CurrencyCode", currencyCode);
        p.Add("@EffectiveFrom", effectiveFrom.Date);
        p.Add("@EffectiveUntil", effectiveUntil?.Date);
        p.Add("@Description", description);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<ArticleDiscount>(
            "sp_ArticleDiscount_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<ArticleDiscountDto>(row);
    }

    public async Task<ArticleDiscountDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<ArticleDiscount>(
            "sp_ArticleDiscount_GetByToken", new { ArticleDiscountToken = token }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            return null;

        EnsureCanManage(context, existing.SupplierId);

        var p = new DynamicParameters();
        p.Add("@ArticleDiscountToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<ArticleDiscount>(
            "sp_ArticleDiscount_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<ArticleDiscountDto>(row);
    }

    public async Task<EffectiveArticleDiscountDto?> GetEffectiveForArticleAsync(int articleId, DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<EffectiveArticleDiscountRow>(
            "sp_ArticleDiscount_GetEffective", new { ArticleId = articleId, AsOfDate = asOfDate.Date }, commandType: CommandType.StoredProcedure);

        if (row is null)
            return null;

        return new EffectiveArticleDiscountDto
        {
            ArticleDiscountToken = row.ArticleDiscountToken,
            DiscountTypeId = row.DiscountTypeId,
            DiscountTypeCode = row.DiscountTypeCode,
            DiscountValue = row.DiscountValue,
            CurrencyCode = row.CurrencyCode,
            ScopeLevel = row.ScopeLevel
        };
    }
}
