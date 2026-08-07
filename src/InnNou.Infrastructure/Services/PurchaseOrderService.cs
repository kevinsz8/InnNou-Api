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

public class PurchaseOrderService(IDbConnectionFactory connectionFactory, IMapper mapper, INotificationService notificationService, ILogger<PurchaseOrderService> logger) : IPurchaseOrderService
{
    private sealed class PurchaseOrderPageRow : PurchaseOrder { public int TotalCount { get; set; } }
    private sealed class GoodsReceiptPageRow : GoodsReceipt { public int TotalCount { get; set; } }

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;
    private const int ApprovalThresholdBatchPageSize = 1000;

    // Read visibility, no RoleLevel floor — matches WarehouseService.CanManageReadAsync. The
    // owning Supplier branch is checked separately by callers before falling back to this.
    private static async Task<bool> CanReadOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId, int? targetWarehouseId = null)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (!context.OrganizationId.HasValue)
            return false;

        if (!WarehouseScopeGuard.Allows(context, targetWarehouseId))
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    // Write visibility (Cancel/Rectify) — only a caller whose own organization is ASSOCIATE may
    // write; SuperAdmin (no organization of their own, unless impersonating) and SUPER_ASSOCIATE
    // are read-only, mirrors OrderService.CanManageOrganizationAsync.
    private static async Task<bool> CanManageOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId, int? targetWarehouseId = null)
    {
        if (context.OrganizationTypeCode != OrganizationTypeCodes.Associate)
            return false;

        if (context.RoleLevel < StaffRoleLevel || !context.OrganizationId.HasValue)
            return false;

        if (!WarehouseScopeGuard.Allows(context, targetWarehouseId))
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    // Always resolves EFFECTIVE values (post any APPLIED rectification) — see
    // sp_PurchaseOrderLine_GetEffective. Narrowed to a single PurchaseOrder via @PurchaseOrderId;
    // @OrderId is still required by the SP (it scopes the "latest APPLIED rectification" lookup
    // exactly the same way regardless of caller, no behavioral difference here).
    private static async Task<List<PurchaseOrderLine>> GetLinesForPurchaseOrderAsync(IDbConnection connection, PurchaseOrder purchaseOrder)
    {
        var lines = await connection.QueryAsync<PurchaseOrderLine>(
            "sp_PurchaseOrderLine_GetEffective",
            new { purchaseOrder.OrderId, purchaseOrder.PurchaseOrderId },
            commandType: CommandType.StoredProcedure);
        return lines.ToList();
    }

    // Best-effort/non-blocking (same convention as every notification call site elsewhere).
    // Recipient is resolved from the originating Order's own CreatedBy (the buyer who submitted
    // it) — never context.ActorUserToken, since receiving/rectifying a PO is normally done by
    // warehouse/ops staff, not the buyer themselves (same reasoning as the Order_Confirmed
    // recipient fix — see .claude/OrderConfirmationModule.md).
    private async Task NotifyOrderBuyerAsync(DbConnection connection, PurchaseOrder purchaseOrder, NotificationType type, object data, IRequestContext context, CancellationToken cancellationToken)
    {
        try
        {
            var order = await connection.QueryFirstOrDefaultAsync<Order>(
                "sp_Order_GetByToken", new { purchaseOrder.OrderToken }, commandType: CommandType.StoredProcedure);

            if (order is null || !Guid.TryParse(order.CreatedBy, out var buyerToken))
                return;

            // notificationService.NotifyAsync opens its own connection — closing this one first
            // keeps at most one connection open at a time on this logical unit of work (Dapper
            // transparently reopens it on the caller's next query). Doesn't matter in production
            // (no ambient transaction), but the integration test harness wraps every test in a
            // System.Transactions.TransactionScope, and two simultaneously-open connections there
            // forces a DTC promotion that isn't configured on a local/CI SQL Server — same
            // reasoning as ApproveStepAndAdvanceAsync's own explicit connection.CloseAsync().
            await connection.CloseAsync();

            await notificationService.NotifyAsync(buyerToken, type, data, $"/orders/{purchaseOrder.OrderToken}", context, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notification failed for PurchaseOrder {PurchaseOrderToken}", purchaseOrder.PurchaseOrderToken);
        }
    }

    public async Task<PagedResult<PurchaseOrderDto>> GetPagedAsync(Guid? organizationToken, Guid? orderToken, string? status, List<string>? statuses, string? purchaseOrderNumber, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        // Purely an additional narrowing filter layered on top of the scope resolved below —
        // never widens what the caller could already see, same rule the RoleIds/OrganizationIds
        // multi-value filter on GetUsers established.
        int? orderId = null;
        if (orderToken.HasValue)
        {
            var order = await connection.QueryFirstOrDefaultAsync<Order>(
                "sp_Order_GetByToken", new { OrderToken = orderToken.Value }, commandType: CommandType.StoredProcedure);

            if (order is null || !WarehouseScopeGuard.Allows(context, order.WarehouseId))
                return new PagedResult<PurchaseOrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            orderId = order.OrderId;
        }

        int? rootOrganizationId = null;
        int? supplierId = null;

        if (context.SupplierId.HasValue)
        {
            supplierId = context.SupplierId.Value;
        }
        else if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            rootOrganizationId = null; // unrestricted
        }
        else if (organizationToken.HasValue)
        {
            var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                "sp_Organization_GetByToken",
                new { OrganizationToken = organizationToken.Value, RootOrganizationId = (int?)null },
                commandType: CommandType.StoredProcedure);

            // No per-warehouse filter exists on this endpoint (unlike Orders'/Inventory's own
            // GetPaged) — an org-wide browse can't be narrowed to just one warehouse, so a
            // warehouse-scoped caller is only ever let through via the orderId-scoped path above.
            if (organization is null || !await CanReadOrganizationAsync(connection, context, organization.OrganizationId) || (context.WarehouseId.HasValue && !orderId.HasValue))
                return new PagedResult<PurchaseOrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            rootOrganizationId = organization.OrganizationId;
        }
        else if (context.OrganizationId.HasValue)
        {
            if (context.WarehouseId.HasValue && !orderId.HasValue)
                return new PagedResult<PurchaseOrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            rootOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            return new PagedResult<PurchaseOrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        int? statusId = null;
        if (status is not null)
        {
            // An unrecognized status filter matches nothing rather than 500ing.
            if (!PurchaseOrderStatusCodes.TryFromCode(status, out var parsedStatus))
                return new PagedResult<PurchaseOrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
            statusId = (int)parsedStatus;
        }

        List<int>? statusIds = null;
        if (statuses is { Count: > 0 })
        {
            statusIds = [];
            foreach (var s in statuses)
            {
                // Same "unrecognized filter matches nothing" rule as the singular Status above.
                if (!PurchaseOrderStatusCodes.TryFromCode(s, out var parsed))
                    return new PagedResult<PurchaseOrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
                statusIds.Add((int)parsed);
            }
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@SupplierId", supplierId);
        p.Add("@OrderId", orderId);
        p.Add("@StatusId", statusId);
        p.Add("@StatusIds", statusIds is { Count: > 0 } ? string.Join(',', statusIds) : null);
        p.Add("@PurchaseOrderNumber", string.IsNullOrWhiteSpace(purchaseOrderNumber) ? null : purchaseOrderNumber.Trim());
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<PurchaseOrderPageRow>(
            "sp_PurchaseOrder_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<PurchaseOrderDto>
        {
            Items = mapper.MapList<PurchaseOrderDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<PurchaseOrderDto?> GetByTokenAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (purchaseOrder is null)
            return null;

        var canView = context.SupplierId.HasValue
            ? context.SupplierId.Value == purchaseOrder.SupplierId
            : await CanReadOrganizationAsync(connection, context, purchaseOrder.OrganizationId, purchaseOrder.WarehouseId);

        if (!canView)
            return null;

        var dto = mapper.Map<PurchaseOrderDto>(purchaseOrder);
        dto.Lines = mapper.MapList<PurchaseOrderLineDto>(
            await GetLinesForPurchaseOrderAsync(connection, purchaseOrder));
        dto.LineCount = dto.Lines.Count;
        return dto;
    }

    public async Task<PurchaseOrderDto?> CancelAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        // Deliberately no Supplier-bypass — this system is buyer-side only. A Supplier can read
        // their own PurchaseOrders/GoodsReceipts but never write to them, same as
        // CreateRectificationAsync and CreateGoodsReceiptAsync.
        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId, existing.WarehouseId))
            throw new ApiException(ErrorCodes.PurchaseOrderForbidden, "Cannot cancel a purchase order outside your scope.", 403);

        if (existing.Status != PurchaseOrderStatus.Sent)
            throw new ApiException(ErrorCodes.PurchaseOrderNotSent, "Only a sent purchase order can be cancelled.", 409);

        var updated = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_Cancel",
            new { PurchaseOrderToken = purchaseOrderToken, CancelledBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        if (updated is null)
            return null;

        var dto = mapper.Map<PurchaseOrderDto>(updated);
        dto.Lines = mapper.MapList<PurchaseOrderLineDto>(
            await GetLinesForPurchaseOrderAsync(connection, updated));
        dto.LineCount = dto.Lines.Count;
        return dto;
    }

    public async Task<PurchaseOrderDto?> CloseShortAsync(Guid purchaseOrderToken, string reason, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        // Deliberately no Supplier-bypass, same as Cancel/Rectify/CreateGoodsReceipt — this
        // system is buyer-side only.
        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId, existing.WarehouseId))
            throw new ApiException(ErrorCodes.PurchaseOrderForbidden, "Cannot close a purchase order outside your scope.", 403);

        if (existing.Status != PurchaseOrderStatus.Partially_Received)
            throw new ApiException(ErrorCodes.PurchaseOrderCloseShortNotAllowed, "Only a partially received purchase order can be closed as short.", 409);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ApiException(ErrorCodes.PurchaseOrderCloseShortReasonRequired, "A reason is required to close a purchase order as short.", 400);

        var updated = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_CloseShort",
            new { PurchaseOrderToken = purchaseOrderToken, ClosedShortBy = context.ActorUserToken.ToString(), ClosedShortReason = reason.Trim() },
            commandType: CommandType.StoredProcedure);

        if (updated is null)
            return null;

        var dto = mapper.Map<PurchaseOrderDto>(updated);
        dto.Lines = mapper.MapList<PurchaseOrderLineDto>(
            await GetLinesForPurchaseOrderAsync(connection, updated));
        dto.LineCount = dto.Lines.Count;
        return dto;
    }

    private sealed class ValidatedRectificationLine
    {
        public required PurchaseOrderLine Line { get; init; }
        public required string Action { get; init; }
        public decimal? NewQuantity { get; init; }
        public decimal? NewUnitPrice { get; init; }
        public string? NewCurrencyCode { get; init; }
    }

    private sealed class TriggeredRectificationApprovalStep
    {
        public int FamilyId { get; set; }
        public string FamilyCode { get; set; } = default!;
        public int Level { get; set; }
        public decimal ThresholdAmount { get; set; }
        public decimal ActualFamilyAmount { get; set; }
        public int ApproverUserId { get; set; }
    }

    // Same shape as OrderService's own private nested classes of the same name — duplicated
    // rather than shared, matching this codebase's established "cross-domain write inside another
    // workflow's transaction stays a raw Dapper call, never a cross-service injection" convention.
    private sealed class SupplierDeliveryZoneCoverage
    {
        public int? WarehouseZoneId { get; set; }
        public bool EnforcementActive { get; set; }
        public bool HasCoverage { get; set; }
    }

    private sealed class ArticleClassificationEffective
    {
        public int? CategoryId { get; set; }
        public string? CategoryCode { get; set; }
        public int? SubCategoryId { get; set; }
        public string? SubCategoryCode { get; set; }
        public bool IsInherited { get; set; }
    }

    // A brand-new line resolved (article/price/packaging/classification) but not yet inserted —
    // same shape sp_PurchaseOrderLine_Create needs, mirroring OrderService.AddLineAsync's own
    // resolution for a draft Order line.
    private sealed class ValidatedNewRectificationLine
    {
        public required Article Article { get; init; }
        public required decimal Quantity { get; init; }
        public required decimal UnitPrice { get; init; }
        public required string CurrencyCode { get; init; }
        public required int ContentUnitId { get; init; }
        public required decimal ContentQuantity { get; init; }
        public ArticleClassificationEffective? Classification { get; init; }
    }

    public async Task<PurchaseOrderRectificationDto?> CreateRectificationAsync(Guid purchaseOrderToken, string reason, string? notes, List<RectifyPurchaseOrderLineInputDto> lines, List<RectifyPurchaseOrderNewLineInputDto> newLines, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (purchaseOrder is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, purchaseOrder.OrganizationId, purchaseOrder.WarehouseId))
            throw new ApiException(ErrorCodes.PurchaseOrderForbidden, "Cannot rectify a purchase order outside your scope.", 403);

        // A rectification is also allowed once receiving has started (not just SENT) — e.g. a
        // supplier formally telling the buyer they can't fulfil the rest of an already-partially-
        // delivered line. Each line's floor against what's already been accepted (below) is what
        // keeps this safe; RECEIVED/CANCELLED purchase orders have nothing left to correct.
        if (purchaseOrder.Status != PurchaseOrderStatus.Sent && purchaseOrder.Status != PurchaseOrderStatus.Partially_Received)
            throw new ApiException(ErrorCodes.PurchaseOrderRectificationInvalidStatus, "Only a sent or partially received purchase order can be rectified.", 409);

        if (lines.Count == 0 && newLines.Count == 0)
            throw new ApiException(ErrorCodes.PurchaseOrderRectificationEmpty, "At least one line must be rectified or added.", 400);

        if (!PurchaseOrderRectificationReasonCodes.TryFromCode(reason, out var normalizedReason))
            throw new ApiException(ErrorCodes.InvalidRequest, "Invalid rectification reason.", 400);

        // Sum of QuantityAccepted per line across every GoodsReceipt already recorded against this
        // PO — the floor a rectification can never cross. Same source CreateGoodsReceiptAsync uses.
        var existingReceiptLines = (await connection.QueryAsync<GoodsReceiptLine>(
            "sp_GoodsReceiptLine_GetByPurchaseOrderId", new { purchaseOrder.PurchaseOrderId }, commandType: CommandType.StoredProcedure)).ToList();

        var alreadyAccepted = existingReceiptLines
            .GroupBy(l => l.PurchaseOrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.QuantityAccepted));

        // Effective lines across the WHOLE originating Order (every sibling PurchaseOrder) — needed
        // both to validate each requested line belongs to THIS PurchaseOrder and isn't already
        // cancelled, and to recompute each affected Family's total for the approval-threshold
        // check below against the same scope the original Submit evaluation used. See
        // .claude/PurchaseOrderRectificationModule.md.
        var allOrderLines = (await connection.QueryAsync<PurchaseOrderLine>(
            "sp_PurchaseOrderLine_GetEffective", new { purchaseOrder.OrderId, PurchaseOrderId = (int?)null }, commandType: CommandType.StoredProcedure)).ToList();

        var thisPoLinesByToken = allOrderLines
            .Where(l => l.PurchaseOrderId == purchaseOrder.PurchaseOrderId)
            .ToDictionary(l => l.PurchaseOrderLineToken);

        var validatedLines = new List<ValidatedRectificationLine>();

        foreach (var input in lines)
        {
            if (!thisPoLinesByToken.TryGetValue(input.PurchaseOrderLineToken, out var line))
                throw new ApiException(ErrorCodes.PurchaseOrderLineNotFound, $"Purchase order line '{input.PurchaseOrderLineToken}' does not belong to this purchase order.", 404);

            if (line.IsCancelled)
                throw new ApiException(ErrorCodes.PurchaseOrderLineAlreadyCancelled, $"The line for article '{line.ArticleName}' is already cancelled.", 409);

            var acceptedForLine = alreadyAccepted.GetValueOrDefault(line.PurchaseOrderLineId);

            if (input.Cancel)
            {
                // A line with anything already accepted against it has already been physically
                // received in part — cancelling it outright would misrepresent that receipt as
                // never having happened. It can only be quantity-reduced (down to what was
                // accepted, at minimum), never cancelled.
                if (acceptedForLine > 0)
                    throw new ApiException(ErrorCodes.PurchaseOrderRectificationBelowAccepted, $"Cannot cancel the line for article '{line.ArticleName}' — {acceptedForLine} has already been received against it.", 409);

                validatedLines.Add(new ValidatedRectificationLine { Line = line, Action = PurchaseOrderRectificationLineActionCodes.LineCancelled });
                continue;
            }

            if (!input.NewQuantity.HasValue || input.NewQuantity.Value <= 0)
                throw new ApiException(ErrorCodes.PurchaseOrderRectificationInvalidQuantity, $"A positive NewQuantity is required for article '{line.ArticleName}'.", 400);
            if (!input.NewUnitPrice.HasValue || input.NewUnitPrice.Value <= 0)
                throw new ApiException(ErrorCodes.PurchaseOrderRectificationInvalidQuantity, $"A positive NewUnitPrice is required for article '{line.ArticleName}'.", 400);

            // The floor that makes rectifying a partially-received PO safe: never below what's
            // physically already in the building for this line.
            if (input.NewQuantity.Value < acceptedForLine)
                throw new ApiException(ErrorCodes.PurchaseOrderRectificationBelowAccepted, $"Cannot rectify article '{line.ArticleName}' to {input.NewQuantity.Value} — {acceptedForLine} has already been received against it.", 409);

            var newCurrencyCode = string.IsNullOrWhiteSpace(input.NewCurrencyCode) ? line.CurrencyCode : input.NewCurrencyCode.Trim().ToUpperInvariant();

            validatedLines.Add(new ValidatedRectificationLine
            {
                Line = line,
                Action = PurchaseOrderRectificationLineActionCodes.QuantityPriceChange,
                NewQuantity = input.NewQuantity,
                NewUnitPrice = input.NewUnitPrice,
                NewCurrencyCode = newCurrencyCode
            });
        }

        // Brand-new lines — an article never on the original PO (e.g. shipped against a phone-in
        // addition), same supplier only. Resolution mirrors OrderService.AddLineAsync's own
        // (article lookup, zone coverage, catalog-or-manual price, packaging, classification
        // snapshot) since this is functionally "add a line," just applied post-send.
        var articleTokensAlreadyOnOrder = thisPoLinesByToken.Values
            .Where(l => !l.IsCancelled)
            .Select(l => l.ArticleToken)
            .ToHashSet();

        var validatedNewLines = new List<ValidatedNewRectificationLine>();

        foreach (var input in newLines)
        {
            if (articleTokensAlreadyOnOrder.Contains(input.ArticleToken))
                throw new ApiException(ErrorCodes.PurchaseOrderRectificationNewLineAlreadyOnOrder, $"Article '{input.ArticleToken}' is already a line on this purchase order — rectify its quantity/price instead of adding it again.", 409);

            // Mark it claimed immediately (not just after the whole loop) so a second entry for
            // the SAME ArticleToken within this same newLines batch trips the check above too —
            // otherwise both would pass validation and insert as separate PurchaseOrderLine rows,
            // silently double-counting that article's spend for the Family-approval recompute.
            articleTokensAlreadyOnOrder.Add(input.ArticleToken);

            if (input.Quantity <= 0)
                throw new ApiException(ErrorCodes.PurchaseOrderRectificationInvalidQuantity, $"A positive Quantity is required for article '{input.ArticleToken}'.", 400);

            // Same "own organization, strict visibility" resolution AddLineAsync uses — never the
            // acting user's own role/supplier identity, so a private-supplier article resolves
            // only for the PO's own legitimate organization.
            var article = await connection.QueryFirstOrDefaultAsync<Article>(
                "sp_Article_GetByToken", new { ArticleToken = input.ArticleToken, OrganizationId = purchaseOrder.OrganizationId, ContextRoleLevel = 0 }, commandType: CommandType.StoredProcedure);

            if (article is null)
                throw new ApiException(ErrorCodes.ArticleNotFound, $"Article '{input.ArticleToken}' not found.", 404);

            if (article.SupplierId != purchaseOrder.SupplierId)
                throw new ApiException(ErrorCodes.PurchaseOrderRectificationNewLineSupplierMismatch, $"Article '{article.Name}' does not belong to this purchase order's supplier.", 409);

            var packagingLevels = (await connection.QueryAsync<ArticlePackagingLevel>(
                "sp_ArticlePackagingLevel_GetByArticleId", new { ArticleId = article.ArticleId }, commandType: CommandType.StoredProcedure)).ToList();
            var definedLevel = packagingLevels.FirstOrDefault(l => l.IsDefinedUnit);
            var totalContentQuantity = packagingLevels.Aggregate(1m, (total, level) => total * level.QuantityInParentUnit);

            var coverage = await connection.QueryFirstOrDefaultAsync<SupplierDeliveryZoneCoverage>(
                "sp_SupplierDeliveryZone_CheckCoverage",
                new { SupplierId = article.SupplierId, purchaseOrder.WarehouseId },
                commandType: CommandType.StoredProcedure);

            if (coverage is not null && coverage.EnforcementActive && !coverage.HasCoverage)
                throw new ApiException(ErrorCodes.ArticleSupplierZoneNotCovered, "This supplier does not deliver to the purchase order's zone.", 409);

            var priceParams = new DynamicParameters();
            priceParams.Add("@ArticleId", article.ArticleId);
            priceParams.Add("@OrganizationId", purchaseOrder.OrganizationId);
            priceParams.Add("@CurrencyCode", null, DbType.AnsiString, size: 10, direction: ParameterDirection.InputOutput);
            priceParams.Add("@AsOfDate", DateTime.UtcNow.Date);

            var priceRow = await connection.QueryFirstOrDefaultAsync<ArticlePrice>(
                "sp_ArticlePrice_GetCurrent", priceParams, commandType: CommandType.StoredProcedure);

            var resolvedCurrencyCode = priceParams.Get<string?>("@CurrencyCode");

            decimal unitPrice;
            string currencyCode;

            if (priceRow is not null)
            {
                unitPrice = priceRow.Price;
                currencyCode = priceRow.CurrencyCode;
            }
            else
            {
                var isServiceOrMixed = article.SupplierType is SupplierType.Service or SupplierType.Mixed;
                if (!isServiceOrMixed)
                {
                    if (resolvedCurrencyCode is null)
                        throw new ApiException(ErrorCodes.ArticlePriceCurrencyRequired, "A currency code could not be determined for this organization.", 400);

                    throw new ApiException(ErrorCodes.ArticlePriceNotFound, $"No current price found for article '{article.Name}'.", 404);
                }

                if (!input.ManualUnitPrice.HasValue || input.ManualUnitPrice.Value <= 0 || string.IsNullOrWhiteSpace(input.ManualCurrencyCode))
                    throw new ApiException(ErrorCodes.ArticlePriceManualRequired, $"Article '{article.Name}' has no catalog price — provide a manual unit price and currency.", 400);

                var normalizedCurrencyCode = input.ManualCurrencyCode.Trim().ToUpperInvariant();
                var currencyExists = await connection.ExecuteScalarAsync<bool>(
                    "sp_Currency_ExistsByCode", new { Code = normalizedCurrencyCode }, commandType: CommandType.StoredProcedure);
                if (!currencyExists)
                    throw new ApiException(ErrorCodes.ArticlePriceInvalidCurrency, "Invalid or inactive currency code.", 400);

                unitPrice = input.ManualUnitPrice.Value;
                currencyCode = normalizedCurrencyCode;
            }

            var classification = await connection.QueryFirstOrDefaultAsync<ArticleClassificationEffective>(
                "sp_ArticleClassification_GetEffectiveForArticle",
                new { ArticleId = article.ArticleId, OrganizationId = purchaseOrder.OrganizationId },
                commandType: CommandType.StoredProcedure);

            validatedNewLines.Add(new ValidatedNewRectificationLine
            {
                Article = article,
                Quantity = input.Quantity,
                UnitPrice = unitPrice,
                CurrencyCode = currencyCode,
                ContentUnitId = definedLevel?.UnitOfMeasureId ?? article.PurchaseUnitId,
                ContentQuantity = totalContentQuantity,
                Classification = classification
            });
        }

        // Recompute each affected Family's total across the WHOLE Order using effective values,
        // with this rectification's proposed changes overlaid on top of the lines they touch —
        // same evaluation shape as OrderService.EvaluateApprovalRequirementAsync. Only levels not
        // already APPROVED for this (OrderId, FamilyId) trigger a fresh step — an earlier
        // Submit's or an earlier rectification's already-cleared levels stay cleared.
        var proposedByLineId = validatedLines.ToDictionary(v => v.Line.PurchaseOrderLineId);

        var currencyParams = new DynamicParameters();
        currencyParams.Add("@OrganizationId", purchaseOrder.OrganizationId);
        currencyParams.Add("@CurrencyCode", null, DbType.AnsiString, size: 10, direction: ParameterDirection.InputOutput);
        await connection.ExecuteAsync("sp_Organization_ResolveCurrencyCode", currencyParams, commandType: CommandType.StoredProcedure);
        var orgCurrencyCode = currencyParams.Get<string?>("@CurrencyCode");

        var familyTotals = new Dictionary<int, decimal>();
        if (orgCurrencyCode is not null)
        {
            foreach (var line in allOrderLines)
            {
                if (!line.FamilyId.HasValue)
                    continue;

                var isCancelled = line.IsCancelled;
                var quantity = line.Quantity;
                var unitPrice = line.UnitPrice;
                var currencyCode = line.CurrencyCode;

                if (proposedByLineId.TryGetValue(line.PurchaseOrderLineId, out var proposed))
                {
                    isCancelled = proposed.Action == PurchaseOrderRectificationLineActionCodes.LineCancelled;
                    if (!isCancelled)
                    {
                        quantity = proposed.NewQuantity!.Value;
                        unitPrice = proposed.NewUnitPrice!.Value;
                        currencyCode = proposed.NewCurrencyCode!;
                    }
                }

                if (isCancelled || currencyCode != orgCurrencyCode)
                    continue;

                familyTotals[line.FamilyId.Value] = familyTotals.GetValueOrDefault(line.FamilyId.Value) + quantity * unitPrice;
            }

            // A brand-new line adds new spend the same as a quantity/price increase does — fold it
            // into the same Family-total recompute so it participates in the identical
            // approval-threshold check.
            foreach (var newLine in validatedNewLines)
            {
                if (!newLine.Article.FamilyId.HasValue || newLine.CurrencyCode != orgCurrencyCode)
                    continue;

                familyTotals[newLine.Article.FamilyId.Value] = familyTotals.GetValueOrDefault(newLine.Article.FamilyId.Value) + newLine.Quantity * newLine.UnitPrice;
            }
        }

        var existingSteps = (await connection.QueryAsync<OrderApprovalStep>(
            "sp_OrderApprovalStep_GetByOrderId", new { purchaseOrder.OrderId }, commandType: CommandType.StoredProcedure)).ToList();

        var newSteps = new List<TriggeredRectificationApprovalStep>();

        // Batched: one call for every configured threshold across the whole organization (omitting
        // @FamilyId, which the SP already treats as "all families" — see
        // sp_FamilyApprovalThreshold_GetPaged.sql) instead of one round trip per distinct Family
        // touched by this rectification.
        var allThresholds = await connection.QueryAsync<FamilyApprovalThreshold>(
            "sp_FamilyApprovalThreshold_GetPaged",
            new { OrganizationId = purchaseOrder.OrganizationId, PageNumber = 1, PageSize = ApprovalThresholdBatchPageSize, FamilyId = (int?)null, IncludeInactive = false },
            commandType: CommandType.StoredProcedure);
        var thresholdsByFamily = allThresholds.ToLookup(t => t.FamilyId);

        foreach (var (familyId, total) in familyTotals)
        {
            var configuredLevels = thresholdsByFamily[familyId].OrderBy(t => t.Level).ToList();

            var highestTriggeredLevel = configuredLevels.Where(t => total >= t.ThresholdAmount).Select(t => (int?)t.Level).DefaultIfEmpty().Max();
            if (!highestTriggeredLevel.HasValue)
                continue;

            var alreadyApprovedLevels = existingSteps
                .Where(s => s.FamilyId == familyId && s.Status == OrderApprovalStepStatus.Approved)
                .Select(s => s.Level)
                .ToHashSet();

            foreach (var level in configuredLevels.Where(t => t.Level <= highestTriggeredLevel.Value && !alreadyApprovedLevels.Contains(t.Level)))
            {
                newSteps.Add(new TriggeredRectificationApprovalStep
                {
                    FamilyId = familyId,
                    FamilyCode = level.FamilyCode,
                    Level = level.Level,
                    ThresholdAmount = level.ThresholdAmount,
                    ActualFamilyAmount = total,
                    ApproverUserId = level.ApproverUserId
                });
            }
        }

        var needsApproval = newSteps.Count > 0;
        var initialStatus = needsApproval ? PurchaseOrderRectificationStatusCodes.PendingApproval : PurchaseOrderRectificationStatusCodes.Applied;
        var actor = context.ActorUserToken.ToString();

        // Header + lines (+ approval steps, if triggered) are inserted atomically — a partial
        // write here would leave an inconsistent rectification (e.g. a header with no lines).
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var headerParams = new DynamicParameters();
            headerParams.Add("@PurchaseOrderRectificationToken", Guid.NewGuid());
            headerParams.Add("@PurchaseOrderId", purchaseOrder.PurchaseOrderId);
            headerParams.Add("@Reason", normalizedReason);
            headerParams.Add("@Notes", notes);
            headerParams.Add("@Status", initialStatus);
            headerParams.Add("@CreatedBy", actor);

            var header = await connection.QueryFirstOrDefaultAsync<PurchaseOrderRectification>(
                "sp_PurchaseOrderRectification_Create", headerParams, transaction, commandType: CommandType.StoredProcedure);

            if (header is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (!needsApproval)
            {
                // Immediately applied — stamp AppliedUtc via the same SetStatus SP the
                // approved-later path uses, keeping one code path for "materialize this
                // rectification" regardless of whether approval was required.
                header = await connection.QueryFirstOrDefaultAsync<PurchaseOrderRectification>(
                    "sp_PurchaseOrderRectification_SetStatus",
                    new { header.PurchaseOrderRectificationId, Status = PurchaseOrderRectificationStatusCodes.Applied },
                    transaction, commandType: CommandType.StoredProcedure) ?? header;

                // A downward quantity correction (or cancelling a not-yet-received line) can
                // retroactively close out a PARTIALLY_RECEIVED purchase order — recompute the
                // exact same way CreateGoodsReceiptAsync does, against the rectified quantities
                // instead of a new receipt. Only runs when the rectification is immediately
                // applied (not pending approval) — the line quantities aren't actually effective
                // yet otherwise, so there's nothing to recompute against.
                // Any brand-new line is, by definition, unreceived — never counts as "fully
                // accepted," same as a freshly rectified-up quantity wouldn't either.
                var everyLineFullyAccepted = validatedNewLines.Count == 0 && thisPoLinesByToken.Values
                    .Where(l => !(proposedByLineId.TryGetValue(l.PurchaseOrderLineId, out var p) && p.Action == PurchaseOrderRectificationLineActionCodes.LineCancelled))
                    .All(l =>
                    {
                        var effectiveQuantity = proposedByLineId.TryGetValue(l.PurchaseOrderLineId, out var p) && p.NewQuantity.HasValue
                            ? p.NewQuantity.Value
                            : l.Quantity;
                        return alreadyAccepted.GetValueOrDefault(l.PurchaseOrderLineId) >= effectiveQuantity;
                    });
                var anyLineAccepted = thisPoLinesByToken.Values.Any(l => alreadyAccepted.GetValueOrDefault(l.PurchaseOrderLineId) > 0);

                var newReceivingStatus = everyLineFullyAccepted
                    ? PurchaseOrderStatusCodes.Received
                    : anyLineAccepted
                        ? PurchaseOrderStatusCodes.PartiallyReceived
                        : PurchaseOrderStatusCodes.Sent;

                await connection.ExecuteAsync(
                    "sp_PurchaseOrder_SetStatus",
                    new { PurchaseOrderToken = purchaseOrderToken, Status = newReceivingStatus },
                    transaction, commandType: CommandType.StoredProcedure);
            }

            foreach (var validated in validatedLines)
            {
                var lineParams = new DynamicParameters();
                lineParams.Add("@PurchaseOrderLineRectificationToken", Guid.NewGuid());
                lineParams.Add("@PurchaseOrderRectificationId", header.PurchaseOrderRectificationId);
                lineParams.Add("@PurchaseOrderLineId", validated.Line.PurchaseOrderLineId);
                lineParams.Add("@Action", validated.Action);
                lineParams.Add("@PreviousQuantity", validated.Line.Quantity);
                lineParams.Add("@NewQuantity", validated.NewQuantity);
                lineParams.Add("@PreviousUnitPrice", validated.Line.UnitPrice);
                lineParams.Add("@NewUnitPrice", validated.NewUnitPrice);
                lineParams.Add("@PreviousCurrencyCode", validated.Line.CurrencyCode);
                lineParams.Add("@NewCurrencyCode", validated.NewCurrencyCode);
                lineParams.Add("@CreatedBy", actor);

                await connection.ExecuteAsync("sp_PurchaseOrderLineRectification_Create", lineParams, transaction, commandType: CommandType.StoredProcedure);
            }

            // Brand-new lines: the PurchaseOrderLine row is inserted right away regardless of
            // approval state (OrderLineId left NULL — it never went through the cart Order's
            // Submit split), but stays invisible to every read path until this rectification is
            // APPLIED — see sp_PurchaseOrderLine_GetEffective's LINE_ADDED filter. A rejected
            // rectification simply leaves it permanently excluded, same "never delete, just never
            // surface" convention a cancelled line already follows.
            foreach (var newLine in validatedNewLines)
            {
                var newLineParams = new DynamicParameters();
                newLineParams.Add("@PurchaseOrderLineToken", Guid.NewGuid());
                newLineParams.Add("@PurchaseOrderId", purchaseOrder.PurchaseOrderId);
                newLineParams.Add("@OrderLineId", (int?)null);
                newLineParams.Add("@ArticleId", newLine.Article.ArticleId);
                newLineParams.Add("@Quantity", newLine.Quantity);
                newLineParams.Add("@PurchaseUnitId", newLine.Article.PurchaseUnitId);
                newLineParams.Add("@PurchaseQuantity", 1m);
                newLineParams.Add("@ContentUnitId", newLine.ContentUnitId);
                newLineParams.Add("@ContentQuantity", newLine.ContentQuantity);
                newLineParams.Add("@UnitPrice", newLine.UnitPrice);
                newLineParams.Add("@CurrencyCode", newLine.CurrencyCode);
                newLineParams.Add("@CategoryId", newLine.Classification?.CategoryId);
                newLineParams.Add("@CategoryCode", newLine.Classification?.CategoryCode);
                newLineParams.Add("@SubCategoryId", newLine.Classification?.SubCategoryId);
                newLineParams.Add("@SubCategoryCode", newLine.Classification?.SubCategoryCode);
                newLineParams.Add("@Notes", (string?)null);
                newLineParams.Add("@CreatedBy", actor);

                var createdLine = await connection.QueryFirstOrDefaultAsync<PurchaseOrderLine>(
                    "sp_PurchaseOrderLine_Create", newLineParams, transaction, commandType: CommandType.StoredProcedure);

                if (createdLine is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                var addedLineParams = new DynamicParameters();
                addedLineParams.Add("@PurchaseOrderLineRectificationToken", Guid.NewGuid());
                addedLineParams.Add("@PurchaseOrderRectificationId", header.PurchaseOrderRectificationId);
                addedLineParams.Add("@PurchaseOrderLineId", createdLine.PurchaseOrderLineId);
                addedLineParams.Add("@Action", PurchaseOrderRectificationLineActionCodes.LineAdded);
                addedLineParams.Add("@PreviousQuantity", (decimal?)null);
                addedLineParams.Add("@NewQuantity", newLine.Quantity);
                addedLineParams.Add("@PreviousUnitPrice", (decimal?)null);
                addedLineParams.Add("@NewUnitPrice", newLine.UnitPrice);
                addedLineParams.Add("@PreviousCurrencyCode", (string?)null);
                addedLineParams.Add("@NewCurrencyCode", newLine.CurrencyCode);
                addedLineParams.Add("@CreatedBy", actor);

                await connection.ExecuteAsync("sp_PurchaseOrderLineRectification_Create", addedLineParams, transaction, commandType: CommandType.StoredProcedure);
            }

            if (needsApproval)
            {
                foreach (var step in newSteps)
                {
                    var stepParams = new DynamicParameters();
                    stepParams.Add("@OrderApprovalStepToken", Guid.NewGuid());
                    stepParams.Add("@OrderId", purchaseOrder.OrderId);
                    stepParams.Add("@FamilyId", step.FamilyId);
                    stepParams.Add("@FamilyCode", step.FamilyCode);
                    stepParams.Add("@Level", step.Level);
                    stepParams.Add("@ThresholdAmount", step.ThresholdAmount);
                    stepParams.Add("@ActualFamilyAmount", step.ActualFamilyAmount);
                    stepParams.Add("@CurrencyCode", orgCurrencyCode);
                    stepParams.Add("@ApproverUserId", step.ApproverUserId);
                    stepParams.Add("@CreatedBy", actor);
                    stepParams.Add("@TriggeringPurchaseOrderRectificationId", header.PurchaseOrderRectificationId);
                    await connection.ExecuteAsync("sp_OrderApprovalStep_Create", stepParams, transaction, commandType: CommandType.StoredProcedure);
                }
            }

            await transaction.CommitAsync(cancellationToken);

            // NotifyOrderBuyerAsync closes/reopens this connection — the committed transaction
            // must be disposed first, or SqlClient throws "The transaction associated with the
            // current connection has completed but has not been disposed" on that Close(), which
            // NotifyOrderBuyerAsync's own try/catch silently swallows, leaving the pooled
            // connection in a bad state for whichever test/request reuses it next.
            await transaction.DisposeAsync();

            await NotifyOrderBuyerAsync(
                connection, purchaseOrder, NotificationType.Purchase_Order_Rectified,
                new { purchaseOrderNumber = purchaseOrder.PurchaseOrderNumber, reason = normalizedReason, needsApproval },
                context, cancellationToken);

            var dto = mapper.Map<PurchaseOrderRectificationDto>(header);
            dto.Lines = mapper.MapList<PurchaseOrderLineRectificationDto>(
                await connection.QueryAsync<PurchaseOrderLineRectification>(
                    "sp_PurchaseOrderLineRectification_GetByRectificationId", new { header.PurchaseOrderRectificationId }, commandType: CommandType.StoredProcedure));

            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<PurchaseOrderRectificationDto>> GetRectificationsAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (purchaseOrder is null)
            return [];

        var canView = context.SupplierId.HasValue
            ? context.SupplierId.Value == purchaseOrder.SupplierId
            : await CanReadOrganizationAsync(connection, context, purchaseOrder.OrganizationId, purchaseOrder.WarehouseId);

        if (!canView)
            return [];

        var headers = (await connection.QueryAsync<PurchaseOrderRectification>(
            "sp_PurchaseOrderRectification_GetByPurchaseOrderId", new { purchaseOrder.PurchaseOrderId }, commandType: CommandType.StoredProcedure)).ToList();

        var result = new List<PurchaseOrderRectificationDto>();
        foreach (var header in headers)
        {
            var dto = mapper.Map<PurchaseOrderRectificationDto>(header);
            dto.Lines = mapper.MapList<PurchaseOrderLineRectificationDto>(
                await connection.QueryAsync<PurchaseOrderLineRectification>(
                    "sp_PurchaseOrderLineRectification_GetByRectificationId", new { header.PurchaseOrderRectificationId }, commandType: CommandType.StoredProcedure));
            result.Add(dto);
        }

        return result;
    }

    private sealed class ValidatedGoodsReceiptLine
    {
        public required PurchaseOrderLine Line { get; init; }
        public required CreateGoodsReceiptLineInputDto Input { get; init; }

        // Resolved (PurchaseUnitId-normalized) quantities — these, not Input.QuantityXxx, are
        // what over-receipt capping, tax, stock deltas, and PO-status recompute must use, so a
        // receipt entered in an alternate unit (see ResolveGoodsReceiptQuantitiesAsync) behaves
        // identically to one entered directly in the Purchase Unit.
        public required decimal QuantityAccepted { get; init; }
        public required decimal QuantityCourtesy { get; init; }
        public required decimal QuantityRejected { get; init; }

        // Raw as-entered mirror — null unless a non-Purchase-Unit UnitToken was supplied. See
        // migrations/20260806_GoodsReceiptLine_UnitConversion.sql.
        public int? EnteredUnitId { get; init; }
        public decimal? AcceptedQuantityInUnit { get; init; }
        public decimal? CourtesyQuantityInUnit { get; init; }
        public decimal? RejectedQuantityInUnit { get; init; }

        // Generated at validation time (before the transaction opens) rather than inside the
        // batch-insert helper, so the same token can be used both to build the
        // GoodsReceiptLineTableType row and, after sp_GoodsReceiptLine_CreateBatch returns, to
        // look up this line's generated GoodsReceiptLineId for the InventoryMovement batch below.
        public Guid GoodsReceiptLineToken { get; } = Guid.NewGuid();
    }

    private readonly record struct ResolvedGoodsReceiptQuantities(
        decimal Accepted, decimal Courtesy, decimal Rejected,
        int? EnteredUnitId, decimal? AcceptedInUnit, decimal? CourtesyInUnit, decimal? RejectedInUnit);

    // Resolves the 3 raw as-entered quantities (all sharing one unit — a receiver counts
    // Accepted/Courtesy/Rejected from the same opened container in the same unit) to
    // PurchaseUnitId-normalized terms. unitToken null (or equal to the Purchase Unit itself) is
    // the default/backward-compatible path — no conversion, no raw mirror stored. Same
    // "resolve once per line, throw ArticleUnitNotValidForArticle on an out-of-chain unit" shape
    // as RequisitionService.ResolveArticleQuantityAsync, generalized to 3 quantities so the
    // packaging chain is only fetched once per line instead of 3 times.
    private static async Task<ResolvedGoodsReceiptQuantities> ResolveGoodsReceiptQuantitiesAsync(
        IDbConnection connection, IMapper mapper, int articleId, int purchaseUnitId, string articleName,
        Guid? unitToken, decimal accepted, decimal courtesy, decimal rejected)
    {
        if (unitToken is null)
            return new ResolvedGoodsReceiptQuantities(accepted, courtesy, rejected, null, null, null, null);

        var unit = await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
            "sp_UnitOfMeasure_GetByToken", new { UnitOfMeasureToken = unitToken.Value }, commandType: CommandType.StoredProcedure);
        if (unit is null)
            throw new ApiException(ErrorCodes.ArticleUnitNotValidForArticle, $"Unit of measure not found for '{articleName}'.", 404);

        if (unit.UnitOfMeasureId == purchaseUnitId)
            return new ResolvedGoodsReceiptQuantities(accepted, courtesy, rejected, null, null, null, null);

        var levels = mapper.MapList<ArticlePackagingLevelDto>(
            await connection.QueryAsync<ArticlePackagingLevel>(
                "sp_ArticlePackagingLevel_GetByArticleId", new { ArticleId = articleId }, commandType: CommandType.StoredProcedure));

        decimal Normalize(decimal quantity)
        {
            var normalized = ArticleUnitConversion.ToPurchaseUnitQuantity(purchaseUnitId, levels, unit.UnitOfMeasureId, quantity);
            if (normalized is null)
                throw new ApiException(ErrorCodes.ArticleUnitNotValidForArticle, $"'{unit.Code}' is not a valid unit for '{articleName}'.", 400);
            return normalized.Value;
        }

        return new ResolvedGoodsReceiptQuantities(
            Normalize(accepted), Normalize(courtesy), Normalize(rejected),
            unit.UnitOfMeasureId, accepted, courtesy, rejected);
    }

    // Batched "how much is that in the article's own Unidad Definida" secondary reference for a
    // receipt's own lines — same anti-N+1 shape as RequisitionService.GetByTokenAsync/
    // InventoryService's own read paths. Computed per quantity (Accepted/Courtesy/Rejected can
    // differ even though they share one EnteredUnitId) — see GoodsReceiptLineDto for the field
    // shape. entities/dtos must be the same list, same order (mapper.MapList preserves order).
    private static async Task PopulateGoodsReceiptDefinedUnitHintsAsync(
        IDbConnection connection, IMapper mapper, List<GoodsReceiptLine> entities, List<GoodsReceiptLineDto> dtos)
    {
        if (entities.Count == 0)
            return;

        var articleIds = entities.Select(e => e.ArticleId).Distinct().ToList();
        var levelRows = await connection.QueryAsync<ArticlePackagingLevel>(
            "sp_ArticlePackagingLevel_GetByArticleIds", new { ArticleIds = string.Join(',', articleIds) }, commandType: CommandType.StoredProcedure);
        var levelsByArticleId = levelRows.GroupBy(l => l.ArticleId)
            .ToDictionary(g => g.Key, g => mapper.MapList<ArticlePackagingLevelDto>(g.ToList()));

        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var dto = dtos[i];
            var levels = levelsByArticleId.GetValueOrDefault(entity.ArticleId, []);
            var effectiveUnitId = entity.EnteredUnitId ?? entity.PurchaseUnitId;

            var acceptedEquivalent = ArticleUnitConversion.GetDefinedUnitEquivalent(entity.PurchaseUnitId, levels, effectiveUnitId, entity.AcceptedQuantityInUnit ?? entity.QuantityAccepted);
            var courtesyEquivalent = ArticleUnitConversion.GetDefinedUnitEquivalent(entity.PurchaseUnitId, levels, effectiveUnitId, entity.CourtesyQuantityInUnit ?? entity.QuantityCourtesy);
            var rejectedEquivalent = ArticleUnitConversion.GetDefinedUnitEquivalent(entity.PurchaseUnitId, levels, effectiveUnitId, entity.RejectedQuantityInUnit ?? entity.QuantityRejected);

            var any = acceptedEquivalent ?? courtesyEquivalent ?? rejectedEquivalent;
            if (any is not null)
            {
                dto.DefinedUnitCode = any.Value.Code;
                dto.DefinedUnitNameTranslations = any.Value.NameTranslations;
            }
            dto.AcceptedDefinedUnitQuantity = acceptedEquivalent?.Quantity;
            dto.CourtesyDefinedUnitQuantity = courtesyEquivalent?.Quantity;
            dto.RejectedDefinedUnitQuantity = rejectedEquivalent?.Quantity;
        }
    }

    private sealed class GoodsReceiptLineIdMapping
    {
        public Guid GoodsReceiptLineToken { get; init; }
        public int GoodsReceiptLineId { get; init; }
    }

    // Column order/types must match dbo.GoodsReceiptLineTableType exactly (see
    // migrations/20260804_GoodsReceiptBatch_CreateTableTypes.sql) — SQL Server matches TVP
    // columns positionally, not by name.
    private static DataTable BuildGoodsReceiptLineTable(int goodsReceiptId, List<ValidatedGoodsReceiptLine> validatedLines, Dictionary<int, GoodsReceiptLineTax> taxByLineId)
    {
        var table = new DataTable();
        table.Columns.Add("GoodsReceiptLineToken", typeof(Guid));
        table.Columns.Add("GoodsReceiptId", typeof(int));
        table.Columns.Add("PurchaseOrderLineId", typeof(int));
        table.Columns.Add("ArticleId", typeof(int));
        table.Columns.Add("QuantityAccepted", typeof(decimal));
        table.Columns.Add("QuantityCourtesy", typeof(decimal));
        table.Columns.Add("QuantityRejected", typeof(decimal));
        table.Columns.Add("RejectionReason", typeof(string));
        table.Columns.Add("LotNumber", typeof(string));
        table.Columns.Add("ExpirationDate", typeof(DateTime));
        table.Columns.Add("SerialNumber", typeof(string));
        table.Columns.Add("Notes", typeof(string));
        table.Columns.Add("UnitPrice", typeof(decimal));
        table.Columns.Add("CurrencyCode", typeof(string));
        table.Columns.Add("TaxCategoryId", typeof(int));
        table.Columns.Add("TaxRateId", typeof(int));
        table.Columns.Add("TaxRatePercent", typeof(decimal));
        table.Columns.Add("TaxableAmount", typeof(decimal));
        table.Columns.Add("TaxAmount", typeof(decimal));
        table.Columns.Add("TotalAmount", typeof(decimal));
        table.Columns.Add("EnteredUnitId", typeof(int));
        table.Columns.Add("AcceptedQuantityInUnit", typeof(decimal));
        table.Columns.Add("CourtesyQuantityInUnit", typeof(decimal));
        table.Columns.Add("RejectedQuantityInUnit", typeof(decimal));

        foreach (var validated in validatedLines)
        {
            var tax = taxByLineId[validated.Line.PurchaseOrderLineId];
            table.Rows.Add(
                validated.GoodsReceiptLineToken,
                goodsReceiptId,
                validated.Line.PurchaseOrderLineId,
                validated.Line.ArticleId,
                validated.QuantityAccepted,
                validated.QuantityCourtesy,
                validated.QuantityRejected,
                (object?)validated.Input.RejectionReason ?? DBNull.Value,
                (object?)validated.Input.LotNumber ?? DBNull.Value,
                (object?)validated.Input.ExpirationDate ?? DBNull.Value,
                (object?)validated.Input.SerialNumber ?? DBNull.Value,
                (object?)validated.Input.Notes ?? DBNull.Value,
                tax.UnitPrice,
                tax.CurrencyCode,
                (object?)tax?.TaxCategoryId ?? DBNull.Value,
                (object?)tax?.TaxRateId ?? DBNull.Value,
                (object?)tax?.TaxRatePercent ?? DBNull.Value,
                (object?)tax?.TaxableAmount ?? DBNull.Value,
                (object?)tax?.TaxAmount ?? DBNull.Value,
                (object?)tax?.TotalAmount ?? DBNull.Value,
                (object?)validated.EnteredUnitId ?? DBNull.Value,
                (object?)validated.AcceptedQuantityInUnit ?? DBNull.Value,
                (object?)validated.CourtesyQuantityInUnit ?? DBNull.Value,
                (object?)validated.RejectedQuantityInUnit ?? DBNull.Value);
        }

        return table;
    }

    // Column order/types must match dbo.StockLevelDeltaTableType exactly.
    private static DataTable BuildStockLevelDeltaTable(int warehouseId, List<(int ArticleId, decimal Delta)> deltas)
    {
        var table = new DataTable();
        table.Columns.Add("WarehouseId", typeof(int));
        table.Columns.Add("ArticleId", typeof(int));
        table.Columns.Add("Delta", typeof(decimal));

        foreach (var (articleId, delta) in deltas)
            table.Rows.Add(warehouseId, articleId, delta);

        return table;
    }

    // Column order/types must match dbo.InventoryMovementTableType exactly.
    private static DataTable BuildInventoryMovementTable(int warehouseId, List<(ValidatedGoodsReceiptLine Validated, decimal StockDelta)> stockedLines, Dictionary<Guid, int> goodsReceiptLineIdByToken)
    {
        var table = new DataTable();
        table.Columns.Add("InventoryMovementToken", typeof(Guid));
        table.Columns.Add("WarehouseId", typeof(int));
        table.Columns.Add("ArticleId", typeof(int));
        table.Columns.Add("Type", typeof(string));
        table.Columns.Add("Quantity", typeof(decimal));
        table.Columns.Add("GoodsReceiptLineId", typeof(int));
        table.Columns.Add("InventoryTransferLineId", typeof(int));
        table.Columns.Add("InventoryPeriodCountId", typeof(int));
        table.Columns.Add("Reason", typeof(string));

        foreach (var (validated, stockDelta) in stockedLines)
        {
            if (!goodsReceiptLineIdByToken.TryGetValue(validated.GoodsReceiptLineToken, out var goodsReceiptLineId))
                throw new InvalidOperationException($"GoodsReceiptLine {validated.GoodsReceiptLineToken} was not returned by sp_GoodsReceiptLine_CreateBatch.");

            table.Rows.Add(
                Guid.NewGuid(),
                warehouseId,
                validated.Line.ArticleId,
                InventoryMovementTypeCodes.Receipt,
                stockDelta,
                goodsReceiptLineId,
                DBNull.Value,
                DBNull.Value,
                DBNull.Value);
        }

        return table;
    }

    private sealed class GoodsReceiptLineTax
    {
        public required decimal UnitPrice { get; init; }
        public required string CurrencyCode { get; init; }
        public int? TaxCategoryId { get; set; }
        public int? TaxRateId { get; set; }
        public decimal? TaxRatePercent { get; set; }
        public decimal? TaxableAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? TotalAmount { get; set; }
    }

    public async Task<GoodsReceiptDto?> CreateGoodsReceiptAsync(Guid purchaseOrderToken, string deliveryNoteNumber, DateTime? deliveryNoteDate, string? notes, List<CreateGoodsReceiptLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (purchaseOrder is null)
            return null;

        // Deliberately NOT the Cancel/Rectify supplier-bypass shape — receiving happens at the
        // buyer's own dock, performed by the buyer's staff. A supplier confirming what arrived
        // at a warehouse they don't operate would be a real access-boundary violation, not a
        // convenience.
        if (!await CanManageOrganizationAsync(connection, context, purchaseOrder.OrganizationId, purchaseOrder.WarehouseId))
            throw new ApiException(ErrorCodes.GoodsReceiptForbidden, "Cannot record a goods receipt for a purchase order outside your scope.", 403);

        if (purchaseOrder.Status != PurchaseOrderStatus.Sent && purchaseOrder.Status != PurchaseOrderStatus.Partially_Received)
            throw new ApiException(ErrorCodes.GoodsReceiptPurchaseOrderNotReceivable, "Only a sent or partially received purchase order can receive goods.", 409);

        if (lines.Count == 0)
            throw new ApiException(ErrorCodes.GoodsReceiptEmpty, "At least one line must be received.", 400);

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { purchaseOrder.WarehouseToken }, commandType: CommandType.StoredProcedure);

        if (warehouse is null || !warehouse.CanReceivePurchases)
            throw new ApiException(ErrorCodes.GoodsReceiptWarehouseCannotReceive, "This warehouse is not configured to receive purchases.", 400);

        var effectiveLines = await GetLinesForPurchaseOrderAsync(connection, purchaseOrder);
        var linesByToken = effectiveLines.ToDictionary(l => l.PurchaseOrderLineToken);

        var existingReceiptLines = (await connection.QueryAsync<GoodsReceiptLine>(
            "sp_GoodsReceiptLine_GetByPurchaseOrderId", new { purchaseOrder.PurchaseOrderId }, commandType: CommandType.StoredProcedure)).ToList();

        var alreadyAccepted = existingReceiptLines
            .GroupBy(l => l.PurchaseOrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.QuantityAccepted));

        var validatedLines = new List<ValidatedGoodsReceiptLine>();
        var requestedLineIds = new HashSet<int>();

        foreach (var input in lines)
        {
            if (!linesByToken.TryGetValue(input.PurchaseOrderLineToken, out var line))
                throw new ApiException(ErrorCodes.GoodsReceiptLineNotFound, $"Purchase order line '{input.PurchaseOrderLineToken}' does not belong to this purchase order.", 404);

            if (!requestedLineIds.Add(line.PurchaseOrderLineId))
                throw new ApiException(ErrorCodes.GoodsReceiptLineNotFound, $"Purchase order line '{input.PurchaseOrderLineToken}' was submitted more than once.", 400);

            if (line.IsCancelled)
                throw new ApiException(ErrorCodes.GoodsReceiptLineAlreadyCancelled, $"The line for article '{line.ArticleName}' was cancelled by a rectification and cannot receive goods.", 409);

            // Resolved to PurchaseUnitId terms up front — every check/computation below (over-
            // receipt cap, tax, stock deltas, PO-status recompute) must use these, not the raw
            // Input.QuantityXxx values, so a receipt entered in an alternate unit behaves
            // identically to one entered directly in the Purchase Unit. See
            // migrations/20260806_GoodsReceiptLine_UnitConversion.sql.
            var resolved = await ResolveGoodsReceiptQuantitiesAsync(
                connection, mapper, line.ArticleId, line.PurchaseUnitId, line.ArticleName ?? line.ArticleToken.ToString(),
                input.UnitToken, input.QuantityAccepted, input.QuantityCourtesy, input.QuantityRejected);

            if (resolved.Accepted < 0 || resolved.Courtesy < 0 || resolved.Rejected < 0)
                throw new ApiException(ErrorCodes.GoodsReceiptLineEmpty, $"Quantities for article '{line.ArticleName}' cannot be negative.", 400);

            if (resolved.Accepted + resolved.Courtesy + resolved.Rejected <= 0)
                throw new ApiException(ErrorCodes.GoodsReceiptLineEmpty, $"At least one quantity must be greater than zero for article '{line.ArticleName}'.", 400);

            var remaining = line.Quantity - alreadyAccepted.GetValueOrDefault(line.PurchaseOrderLineId);
            if (resolved.Accepted > remaining)
                throw new ApiException(ErrorCodes.GoodsReceiptOverReceiptNotAllowed, $"Cannot accept {resolved.Accepted} for article '{line.ArticleName}' — only {remaining} remains to receive. Any supplier surplus must be recorded as Courtesy or Rejected.", 400);

            if (resolved.Accepted > 0 && warehouse.TrackLotNumbers && string.IsNullOrWhiteSpace(input.LotNumber))
                throw new ApiException(ErrorCodes.GoodsReceiptLotNumberRequired, $"A lot number is required for article '{line.ArticleName}' at this warehouse.", 400);

            if (resolved.Accepted > 0 && warehouse.TrackExpirationDates && !input.ExpirationDate.HasValue)
                throw new ApiException(ErrorCodes.GoodsReceiptExpirationDateRequired, $"An expiration date is required for article '{line.ArticleName}' at this warehouse.", 400);

            if (resolved.Accepted > 0 && warehouse.TrackSerialNumbers && string.IsNullOrWhiteSpace(input.SerialNumber))
                throw new ApiException(ErrorCodes.GoodsReceiptSerialNumberRequired, $"A serial number is required for article '{line.ArticleName}' at this warehouse.", 400);

            if (resolved.Rejected > 0 && string.IsNullOrWhiteSpace(input.RejectionReason))
                throw new ApiException(ErrorCodes.GoodsReceiptRejectionReasonRequired, $"A rejection reason is required for article '{line.ArticleName}'.", 400);

            validatedLines.Add(new ValidatedGoodsReceiptLine
            {
                Line = line,
                Input = input,
                QuantityAccepted = resolved.Accepted,
                QuantityCourtesy = resolved.Courtesy,
                QuantityRejected = resolved.Rejected,
                EnteredUnitId = resolved.EnteredUnitId,
                AcceptedQuantityInUnit = resolved.AcceptedInUnit,
                CourtesyQuantityInUnit = resolved.CourtesyInUnit,
                RejectedQuantityInUnit = resolved.RejectedInUnit
            });
        }

        // UnitPrice is frozen for every received line unconditionally — it's just "what we agreed
        // to pay per unit," no tax dependency at all, so it's always knowable regardless of the
        // Accepted/Courtesy/Rejected split. Needed so a future Nota de Crédito (or any other
        // consumer) can value a rejected/returned quantity even when tax was never configured for
        // this warehouse. See .claude/ArticleUnitConversionModule.md's "Price comparison report"
        // section for the sibling finding that started this — a 100%-rejected line used to freeze
        // nothing at all, leaving no price to compute a credit from.
        var taxByLineId = validatedLines.ToDictionary(
            v => v.Line.PurchaseOrderLineId,
            v => new GoodsReceiptLineTax { UnitPrice = v.Line.UnitPrice, CurrencyCode = v.Line.CurrencyCode });

        // TaxableAmount/TaxAmount/TotalAmount are computed only against the billable quantity
        // (QuantityAccepted) — Courtesy is a supplier-gifted surplus with no monetary value to
        // tax, and Rejected is never billed. Missing tax configuration is a HARD block only for a
        // billable line (unchanged behavior — "must configure tax before receiving billable
        // goods"), never for a rejected-only line, so a warehouse that hasn't set up tax yet can
        // still register an all-rejected delivery exactly as it always could.
        var billableLines = validatedLines.Where(v => v.QuantityAccepted > 0).ToList();
        if (billableLines.Count > 0 && !warehouse.TaxJurisdictionId.HasValue)
            throw new ApiException(ErrorCodes.GoodsReceiptWarehouseTaxJurisdictionMissing, "This warehouse has no tax jurisdiction configured — set one before receiving billable goods.", 400);

        // Widened from "billable lines only" to "every priced line" (Accepted or Rejected > 0) so
        // a rejected-only line also gets a real TaxCategoryId/TaxRateId/TaxRatePercent frozen when
        // tax IS configured — needed to compute what a credit note for it would owe. A rejected-
        // only line's own missing category/rate is never a hard error (best-effort only, unlike
        // the billable path below) — receiving must never fail just because Fase C's own
        // convenience data can't be resolved for one article.
        var pricedLines = validatedLines.Where(v => v.QuantityAccepted > 0 || v.QuantityRejected > 0).ToList();
        if (pricedLines.Count > 0 && warehouse.TaxJurisdictionId.HasValue)
        {
            var distinctArticleIds = pricedLines.Select(v => v.Line.ArticleId).Distinct().ToList();
            var effectiveCategories = (await connection.QueryAsync<ArticleEffectiveTaxCategory>(
                "sp_Article_GetEffectiveTaxCategoryByIds",
                new { ArticleIds = string.Join(",", distinctArticleIds), warehouse.TaxJurisdictionId },
                commandType: CommandType.StoredProcedure)).ToDictionary(a => a.ArticleId);

            var rates = (await connection.QueryAsync<TaxRate>(
                "sp_TaxRate_GetByJurisdictionId",
                new { warehouse.TaxJurisdictionId },
                commandType: CommandType.StoredProcedure)).ToDictionary(r => r.TaxCategoryId);

            foreach (var validated in pricedLines)
            {
                var isBillable = validated.QuantityAccepted > 0;

                if (!effectiveCategories.TryGetValue(validated.Line.ArticleId, out var effective) || !effective.TaxCategoryId.HasValue)
                {
                    if (isBillable)
                        throw new ApiException(ErrorCodes.GoodsReceiptArticleTaxCategoryMissing, $"Article '{validated.Line.ArticleName}' has no tax category configured (directly or via its Family) — configure one before receiving billable goods.", 400);
                    continue;
                }

                if (!rates.TryGetValue(effective.TaxCategoryId.Value, out var rate))
                {
                    if (isBillable)
                        throw new ApiException(ErrorCodes.GoodsReceiptTaxRateMissing, $"No tax rate is configured for category '{effective.TaxCategoryCode}' in this warehouse's tax jurisdiction.", 400);
                    continue;
                }

                var taxableAmount = validated.QuantityAccepted * validated.Line.UnitPrice;
                var taxAmount = Math.Round(taxableAmount * rate.RatePercent / 100m, 8);

                var tax = taxByLineId[validated.Line.PurchaseOrderLineId];
                tax.TaxCategoryId = effective.TaxCategoryId.Value;
                tax.TaxRateId = rate.TaxRateId;
                tax.TaxRatePercent = rate.RatePercent;
                tax.TaxableAmount = taxableAmount;
                tax.TaxAmount = taxAmount;
                tax.TotalAmount = taxableAmount + taxAmount;
            }
        }

        var acceptedByLineId = validatedLines.ToDictionary(v => v.Line.PurchaseOrderLineId, v => v.QuantityAccepted);
        var everyLineFullyAccepted = effectiveLines
            .Where(l => !l.IsCancelled)
            .All(l => alreadyAccepted.GetValueOrDefault(l.PurchaseOrderLineId) + acceptedByLineId.GetValueOrDefault(l.PurchaseOrderLineId) >= l.Quantity);
        var anyLineAccepted = effectiveLines
            .Any(l => alreadyAccepted.GetValueOrDefault(l.PurchaseOrderLineId) + acceptedByLineId.GetValueOrDefault(l.PurchaseOrderLineId) > 0);

        var newStatus = everyLineFullyAccepted
            ? PurchaseOrderStatusCodes.Received
            : anyLineAccepted
                ? PurchaseOrderStatusCodes.PartiallyReceived
                : PurchaseOrderStatusCodes.Sent;

        var actor = context.ActorUserToken.ToString();

        // Header + lines + the PurchaseOrder status recompute are inserted/updated atomically —
        // a partial write here would leave a receipt whose lines don't match the PO's reported
        // status. Same shape as CreateRectificationAsync above.
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var headerParams = new DynamicParameters();
            headerParams.Add("@GoodsReceiptToken", Guid.NewGuid());
            headerParams.Add("@PurchaseOrderId", purchaseOrder.PurchaseOrderId);
            headerParams.Add("@WarehouseId", purchaseOrder.WarehouseId);
            headerParams.Add("@DeliveryNoteNumber", deliveryNoteNumber);
            headerParams.Add("@DeliveryNoteDate", deliveryNoteDate?.Date);
            headerParams.Add("@Notes", notes);
            headerParams.Add("@CreatedBy", actor);

            var header = await connection.QueryFirstOrDefaultAsync<GoodsReceipt>(
                "sp_GoodsReceipt_Create", headerParams, transaction, commandType: CommandType.StoredProcedure);

            if (header is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            // GoodsReceiptLine insert, StockLevel delta application, and InventoryMovement
            // insert — each previously one round trip per accepted line (up to 3) — are now one
            // batch call each via table-valued parameters (see
            // migrations/20260804_GoodsReceiptBatch_CreateTableTypes.sql). This codebase's first
            // two TVP-based batches (alongside sp_PurchaseOrderLine_CreateBatch), reserved for
            // exactly this shape: a per-line write loop on a hot path (every receipt) where
            // STRING_SPLIT-style scalar batching (used everywhere else for reads) doesn't apply.
            var linesTable = BuildGoodsReceiptLineTable(header.GoodsReceiptId, validatedLines, taxByLineId);
            var lineBatchParams = new DynamicParameters();
            lineBatchParams.Add("@Lines", linesTable.AsTableValuedParameter("dbo.GoodsReceiptLineTableType"));
            lineBatchParams.Add("@CreatedBy", actor);
            var goodsReceiptLineIdByToken = (await connection.QueryAsync<GoodsReceiptLineIdMapping>(
                "sp_GoodsReceiptLine_CreateBatch", lineBatchParams, transaction, commandType: CommandType.StoredProcedure))
                .ToDictionary(m => m.GoodsReceiptLineToken, m => m.GoodsReceiptLineId);

            // Stock effect — the RECEIPT trigger Inventory's own module doc describes. Both
            // Accepted and Courtesy are real physical stock regardless of billing; Rejected
            // never touches stock. Skipped entirely (not a failure) when the warehouse doesn't
            // track inventory — receiving is still recorded in GoodsReceipt itself, stock
            // tracking is an optional side effect. See .claude/InventoryModule.md. Deltas are
            // summed per ArticleId (not one row per line) — sp_StockLevel_ApplyDeltaBatch's
            // MERGE requires each target (Warehouse,Article) row to be matched at most once.
            if (warehouse.IsInventoriable)
            {
                var stockedLines = validatedLines
                    .Select(v => (Validated: v, StockDelta: v.QuantityAccepted + v.QuantityCourtesy))
                    .Where(x => x.StockDelta > 0)
                    .ToList();

                if (stockedLines.Count > 0)
                {
                    var deltasByArticle = stockedLines
                        .GroupBy(x => x.Validated.Line.ArticleId)
                        .Select(g => (ArticleId: g.Key, Delta: g.Sum(x => x.StockDelta)))
                        .ToList();

                    var deltaTable = BuildStockLevelDeltaTable(warehouse.WarehouseId, deltasByArticle);
                    var deltaParams = new DynamicParameters();
                    deltaParams.Add("@Deltas", deltaTable.AsTableValuedParameter("dbo.StockLevelDeltaTableType"));
                    deltaParams.Add("@ActorBy", actor);
                    await connection.ExecuteAsync(
                        "sp_StockLevel_ApplyDeltaBatch", deltaParams, transaction, commandType: CommandType.StoredProcedure);

                    var movementTable = BuildInventoryMovementTable(warehouse.WarehouseId, stockedLines, goodsReceiptLineIdByToken);
                    var movementBatchParams = new DynamicParameters();
                    movementBatchParams.Add("@Movements", movementTable.AsTableValuedParameter("dbo.InventoryMovementTableType"));
                    movementBatchParams.Add("@CreatedBy", actor);
                    await connection.ExecuteAsync(
                        "sp_InventoryMovement_CreateBatch", movementBatchParams, transaction, commandType: CommandType.StoredProcedure);
                }
            }

            await connection.ExecuteAsync(
                "sp_PurchaseOrder_SetStatus",
                new { PurchaseOrderToken = purchaseOrderToken, Status = newStatus },
                transaction, commandType: CommandType.StoredProcedure);

            await transaction.CommitAsync(cancellationToken);

            // See the identical comment in CreateRectificationAsync above — NotifyOrderBuyerAsync
            // closes/reopens this connection, so the committed transaction must be disposed first.
            await transaction.DisposeAsync();

            await NotifyOrderBuyerAsync(
                connection, purchaseOrder, NotificationType.Goods_Receipt_Created,
                new { purchaseOrderNumber = purchaseOrder.PurchaseOrderNumber, deliveryNoteNumber },
                context, cancellationToken);

            var dto = mapper.Map<GoodsReceiptDto>(header);
            var createdLineEntities = (await connection.QueryAsync<GoodsReceiptLine>(
                "sp_GoodsReceiptLine_GetByGoodsReceiptId", new { header.GoodsReceiptId }, commandType: CommandType.StoredProcedure)).ToList();
            dto.Lines = mapper.MapList<GoodsReceiptLineDto>(createdLineEntities);
            await PopulateGoodsReceiptDefinedUnitHintsAsync(connection, mapper, createdLineEntities, dto.Lines);
            dto.LineCount = dto.Lines.Count;

            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<GoodsReceiptTaxPreviewLineDto>?> GetGoodsReceiptTaxPreviewAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (purchaseOrder is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, purchaseOrder.OrganizationId, purchaseOrder.WarehouseId))
            throw new ApiException(ErrorCodes.GoodsReceiptForbidden, "Cannot preview tax for a purchase order outside your scope.", 403);

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { purchaseOrder.WarehouseToken }, commandType: CommandType.StoredProcedure);

        var effectiveLines = (await GetLinesForPurchaseOrderAsync(connection, purchaseOrder)).Where(l => !l.IsCancelled).ToList();
        var result = effectiveLines.Select(l => new GoodsReceiptTaxPreviewLineDto { PurchaseOrderLineToken = l.PurchaseOrderLineToken }).ToList();

        // Same "never fabricate a value the system doesn't actually know" rule as everywhere
        // else — an unconfigured warehouse jurisdiction/article category/tax rate just leaves
        // TaxCategoryCode/TaxRatePercent null here (never throws, unlike the real submission
        // path in CreateGoodsReceiptAsync) so the receiving page can show "-" instead of a
        // fabricated rate or a blocked page.
        if (warehouse?.TaxJurisdictionId is null || effectiveLines.Count == 0)
            return result;

        var distinctArticleIds = effectiveLines.Select(l => l.ArticleId).Distinct().ToList();
        var effectiveCategories = (await connection.QueryAsync<ArticleEffectiveTaxCategory>(
            "sp_Article_GetEffectiveTaxCategoryByIds",
            new { ArticleIds = string.Join(",", distinctArticleIds), warehouse.TaxJurisdictionId },
            commandType: CommandType.StoredProcedure)).ToDictionary(a => a.ArticleId);

        var rates = (await connection.QueryAsync<TaxRate>(
            "sp_TaxRate_GetByJurisdictionId",
            new { warehouse.TaxJurisdictionId },
            commandType: CommandType.StoredProcedure)).ToDictionary(r => r.TaxCategoryId);

        foreach (var (line, preview) in effectiveLines.Zip(result))
        {
            if (!effectiveCategories.TryGetValue(line.ArticleId, out var effective) || !effective.TaxCategoryId.HasValue)
                continue;

            if (!rates.TryGetValue(effective.TaxCategoryId.Value, out var rate))
                continue;

            preview.TaxCategoryCode = effective.TaxCategoryCode;
            preview.TaxRatePercent = rate.RatePercent;
        }

        return result;
    }

    public async Task<PagedResult<GoodsReceiptDto>> GetGoodsReceiptsAsync(Guid? purchaseOrderToken, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        int? purchaseOrderId = null;
        int? rootOrganizationId = null;
        int? supplierId = null;

        if (purchaseOrderToken.HasValue)
        {
            var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
                "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken.Value }, commandType: CommandType.StoredProcedure);

            if (purchaseOrder is null)
                return new PagedResult<GoodsReceiptDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            var canView = context.SupplierId.HasValue
                ? context.SupplierId.Value == purchaseOrder.SupplierId
                : await CanReadOrganizationAsync(connection, context, purchaseOrder.OrganizationId, purchaseOrder.WarehouseId);

            if (!canView)
                return new PagedResult<GoodsReceiptDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            purchaseOrderId = purchaseOrder.PurchaseOrderId;
        }
        else if (context.SupplierId.HasValue)
        {
            supplierId = context.SupplierId.Value;
        }
        else if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            rootOrganizationId = null; // unrestricted
        }
        else if (context.OrganizationId.HasValue)
        {
            // No per-warehouse filter exists on this unscoped-browse path — a warehouse-scoped
            // caller is only ever let through via the purchaseOrderToken-scoped branch above.
            if (context.WarehouseId.HasValue)
                return new PagedResult<GoodsReceiptDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            rootOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            return new PagedResult<GoodsReceiptDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@SupplierId", supplierId);
        p.Add("@PurchaseOrderId", purchaseOrderId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<GoodsReceiptPageRow>(
            "sp_GoodsReceipt_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        var items = mapper.MapList<GoodsReceiptDto>(rows);

        // Unlike PurchaseOrder's own GetPagedAsync, hydrate Lines for every row here — a
        // GoodsReceipt list is always scoped to a handful of receipts for one PurchaseOrder
        // (never an unbounded cross-organization browse — GetGoodsReceiptsQueryHandler requires
        // PurchaseOrderToken and rejects the request otherwise, precisely so this per-row
        // hydration stays safe), and the caller (the "Receive" modal) needs every line's
        // QuantityAccepted to compute what's already been received.
        foreach (var item in items)
        {
            var goodsReceipt = rows.First(r => r.GoodsReceiptToken == item.GoodsReceiptToken);
            var lineEntities = (await connection.QueryAsync<GoodsReceiptLine>(
                "sp_GoodsReceiptLine_GetByGoodsReceiptId", new { goodsReceipt.GoodsReceiptId }, commandType: CommandType.StoredProcedure)).ToList();
            item.Lines = mapper.MapList<GoodsReceiptLineDto>(lineEntities);
            await PopulateGoodsReceiptDefinedUnitHintsAsync(connection, mapper, lineEntities, item.Lines);
        }

        return new PagedResult<GoodsReceiptDto>
        {
            Items = items,
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<PagedResult<GoodsReceiptSummaryDto>> GetGoodsReceiptsPagedAsync(Guid? organizationToken, Guid? warehouseToken, string? purchaseOrderNumber, string? deliveryNoteNumber, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        // Defaults to the caller's own WarehouseId (WarehouseContact login) so an unfiltered
        // request never falls through to "every warehouse in the org" — an explicit
        // warehouseToken is still validated against it below.
        int? warehouseId = context.WarehouseId;
        if (warehouseToken.HasValue)
        {
            var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
                "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken.Value }, commandType: CommandType.StoredProcedure);

            if (warehouse is null || !WarehouseScopeGuard.Allows(context, warehouse.WarehouseId))
                return new PagedResult<GoodsReceiptSummaryDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            warehouseId = warehouse.WarehouseId;
        }

        int? rootOrganizationId = null;
        int? supplierId = null;

        if (context.SupplierId.HasValue)
        {
            supplierId = context.SupplierId.Value;
        }
        else if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            rootOrganizationId = null; // unrestricted
        }
        else if (organizationToken.HasValue)
        {
            var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                "sp_Organization_GetByToken",
                new { OrganizationToken = organizationToken.Value, RootOrganizationId = (int?)null },
                commandType: CommandType.StoredProcedure);

            if (organization is null || !await CanReadOrganizationAsync(connection, context, organization.OrganizationId))
                return new PagedResult<GoodsReceiptSummaryDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            rootOrganizationId = organization.OrganizationId;
        }
        else if (context.OrganizationId.HasValue)
        {
            rootOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            return new PagedResult<GoodsReceiptSummaryDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@SupplierId", supplierId);
        p.Add("@WarehouseId", warehouseId);
        p.Add("@PurchaseOrderNumber", string.IsNullOrWhiteSpace(purchaseOrderNumber) ? null : purchaseOrderNumber.Trim());
        p.Add("@DeliveryNoteNumber", string.IsNullOrWhiteSpace(deliveryNoteNumber) ? null : deliveryNoteNumber.Trim());
        p.Add("@FromDate", fromDate?.Date);
        p.Add("@ToDate", toDate?.Date);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<GoodsReceiptSummary>(
            "sp_GoodsReceipt_GetPagedSummary", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<GoodsReceiptSummaryDto>
        {
            Items = mapper.MapList<GoodsReceiptSummaryDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

}
