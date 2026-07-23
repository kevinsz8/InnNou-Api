using ClosedXML.Excel;
using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Documents;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Localization;
using InnNou.Shared.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace InnNou.Infrastructure.Services;

public class OrderService(
    IDbConnectionFactory connectionFactory,
    IMapper mapper,
    IOrderPdfStorage orderPdfStorage,
    IEmailSender emailSender,
    ILogger<OrderService> logger,
    IConfiguration configuration) : IOrderService
{
    private sealed class OrderPageRow : Order { public int TotalCount { get; set; } }

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;
    private const int MaxBulkImportRows = 500;

    // RoleLevel >= 100 manages any organization. RoleLevel >= 20 (Staff) manages only within
    // their own organization's hierarchy (root-or-descendant). Below that, or with no
    // OrganizationId (e.g. a Supplier-scoped login), no access at all — never unrestricted.
    // This is read/write-neutral hierarchy access — used directly by GetByTokenAsync (reads),
    // and as the base for CanManageOrganizationAsync (writes) below.
    private static async Task<bool> CanAccessOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (context.RoleLevel < StaffRoleLevel || !context.OrganizationId.HasValue)
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    // Only a caller whose own organization is ASSOCIATE (the property level) may write — both
    // SuperAdmin (no organization of their own, unless impersonating) and SUPER_ASSOCIATE (the
    // holding/group level) are read-only for Orders, no exceptions; purchasing happens at the
    // ASSOCIATE level only. The holding org and SuperAdmin can still see everything
    // (GetPagedAsync/GetByTokenAsync are unaffected — they call CanAccessOrganizationAsync
    // directly, which keeps its own RoleLevel>=100 bypass for reads) but never
    // creates/edits/submits/cancels/deletes an order itself. Impersonating an ASSOCIATE
    // organization's user (or one of its warehouse contacts) flips the effective
    // OrganizationTypeCode to ASSOCIATE for that session, which is what grants write access —
    // not the RoleLevel. Used to gate every write call site (Create/AddLine/EditLine/
    // DeleteLine/Submit/Cancel/Delete).
    private static async Task<bool> CanManageOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.OrganizationTypeCode != OrganizationTypeCodes.Associate)
            return false;

        if (context.RoleLevel < StaffRoleLevel || !context.OrganizationId.HasValue)
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    private static async Task<List<OrderLine>> GetLinesAsync(IDbConnection connection, int orderId)
    {
        var lines = await connection.QueryAsync<OrderLine>(
            "sp_OrderLine_GetByOrderId", new { OrderId = orderId }, commandType: CommandType.StoredProcedure);
        return lines.ToList();
    }

    private static async Task<List<OrderApprovalStep>> GetApprovalStepsAsync(IDbConnection connection, int orderId)
    {
        var steps = await connection.QueryAsync<OrderApprovalStep>(
            "sp_OrderApprovalStep_GetByOrderId", new { OrderId = orderId }, commandType: CommandType.StoredProcedure);
        return steps.ToList();
    }

    // Name + LanguageCode in one round trip — the order confirmation email/PDF sent to the buyer
    // is localized via the buying Organization's own LanguageCode (OrderConfirmationLocalization
    // falls back to "en" for a null/unrecognized code). The supplier-facing "New purchase order"
    // email/PDF is localized the same way via PurchaseOrder.SupplierLanguageCode below.
    private static async Task<(string Name, string? LanguageCode)> GetOrganizationNameAndLanguageAsync(IDbConnection connection, Guid organizationToken)
    {
        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken", new { OrganizationToken = organizationToken }, commandType: CommandType.StoredProcedure);
        return (organization?.Name ?? "InnNou", organization?.LanguageCode);
    }

    private static async Task<string?> GetUserEmailAsync(IDbConnection connection, Guid userToken)
    {
        var user = await connection.QueryFirstOrDefaultAsync<User>(
            "sp_User_GetByToken", new { UserToken = userToken }, commandType: CommandType.StoredProcedure);
        return user?.Email;
    }

    // The order confirmation PDF/email header shows the delivery warehouse's own address and
    // primary contact (Name/Phone/Email) — sp_WarehouseContact_GetPagedByWarehouseId already
    // orders by IsPrimary DESC, ContactName, so PageSize=1 hands back exactly the primary contact
    // (or the first active one alphabetically if none is marked primary). Missing address/contact
    // data simply means those fields are null — OrderConfirmationDocument/EmailContent skip the
    // corresponding labeled row rather than printing a blank one.
    private static async Task<OrderConfirmationData.WarehouseHeaderInfo> GetWarehouseHeaderInfoAsync(IDbConnection connection, Guid warehouseToken)
    {
        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);

        WarehouseContact? contact = null;
        if (warehouse is not null)
        {
            contact = await connection.QueryFirstOrDefaultAsync<WarehouseContact>(
                "sp_WarehouseContact_GetPagedByWarehouseId",
                new { WarehouseId = warehouse.WarehouseId, PageNumber = 1, PageSize = 1, SearchText = (string?)null, IncludeInactive = false },
                commandType: CommandType.StoredProcedure);
        }

        return new OrderConfirmationData.WarehouseHeaderInfo
        {
            AddressLine1 = warehouse?.AddressLine1,
            AddressLine2 = warehouse?.AddressLine2,
            City = warehouse?.City,
            State = warehouse?.State,
            PostalCode = warehouse?.PostalCode,
            Country = warehouse?.Country,
            ContactName = contact?.ContactName,
            ContactPhone = contact?.Phone ?? contact?.Mobile,
            ContactEmail = contact?.Email
        };
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(Guid? warehouseToken, string? status, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        int? rootOrganizationId;
        if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            rootOrganizationId = null;
        }
        else if (context.RoleLevel >= StaffRoleLevel && context.OrganizationId.HasValue)
        {
            rootOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            return new PagedResult<OrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        int? warehouseId = null;
        if (warehouseToken.HasValue)
        {
            var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
                "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken.Value }, commandType: CommandType.StoredProcedure);
            if (warehouse is null)
                return new PagedResult<OrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
            warehouseId = warehouse.WarehouseId;
        }

        int? statusId = null;
        if (status is not null)
        {
            // An unrecognized status filter matches nothing rather than 500ing.
            if (!OrderStatusCodes.TryFromCode(status, out var parsedStatus))
                return new PagedResult<OrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
            statusId = (int)parsedStatus;
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@WarehouseId", warehouseId);
        p.Add("@StatusId", statusId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<OrderPageRow>(
            "sp_Order_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<OrderDto>
        {
            Items = mapper.MapList<OrderDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<OrderDto?> GetByTokenAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        if (order is null || !await CanAccessOrganizationAsync(connection, context, order.OrganizationId))
            return null;

        var dto = mapper.Map<OrderDto>(order);
        dto.Lines = mapper.MapList<OrderLineDto>(await GetLinesAsync(connection, order.OrderId));
        dto.LineCount = dto.Lines.Count;
        dto.ApprovalSteps = mapper.MapList<OrderApprovalStepDto>(await GetApprovalStepsAsync(connection, order.OrderId));
        return dto;
    }

    // Streams the order-confirmation PDF back through the authenticated API — deliberately never
    // served as a static file (unlike Supplier.LogoUrl) since it carries prices/commercial data.
    // Same hierarchy check as GetByTokenAsync (read access, not the stricter CanManage write
    // check); returns null for "order not found or no PDF has been generated yet", which the
    // caller maps to 404.
    public async Task<(byte[] FileBytes, string FileName)?> GetPdfAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            return null;

        if (!await CanAccessOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot access an order outside your organization.", 403);

        if (string.IsNullOrWhiteSpace(order.PdfUrl))
            return null;

        var pdfBytes = await orderPdfStorage.GetAsync(order.OrderToken, cancellationToken);
        return pdfBytes is null ? null : (pdfBytes, $"order-{order.OrderToken:N}.pdf");
    }

    public async Task<OrderDto?> CreateAsync(Guid warehouseToken, string? notes, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);

        if (warehouse is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, warehouse.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot create an order for a warehouse outside your organization.", 403);

        var p = new DynamicParameters();
        p.Add("@OrderToken", Guid.NewGuid());
        p.Add("@OrganizationId", warehouse.OrganizationId);
        p.Add("@WarehouseId", warehouse.WarehouseId);
        p.Add("@Notes", notes);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        var created = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_Create", p, commandType: CommandType.StoredProcedure);

        return created is null ? null : mapper.Map<OrderDto>(created);
    }

    public async Task<OrderLineDto?> AddLineAsync(Guid orderToken, Guid articleToken, decimal quantity, decimal? manualUnitPrice, string? manualCurrencyCode, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot modify an order outside your organization.", 403);

        if (order.Status != OrderStatus.Draft)
            throw new ApiException(ErrorCodes.OrderNotDraft, "Only a draft order can be modified.", 409);

        // Pass the Order's OWN organization (never null, never the acting user's role/supplier
        // identity) so supplier visibility is checked strictly against the order's org — a
        // private-supplier article must resolve here for its legitimate owning organization
        // regardless of who's acting, and must never be reachable via this path for an org that
        // can't see it (ContextRoleLevel/ContextSupplierId deliberately left unset — i.e. 0/NULL
        // — so no acting-user identity can bypass this check; see CLAUDE.md, "Supplier
        // global/private scoping").
        var article = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = articleToken, OrganizationId = order.OrganizationId, ContextRoleLevel = 0 }, commandType: CommandType.StoredProcedure);

        if (article is null)
            throw new ApiException(ErrorCodes.ArticleNotFound, "Article not found.", 404);

        // OrderLine no longer duplicates the Article's full packaging chain (that's now N rows
        // in ArticlePackagingLevels, immutable via Supersede same as PurchaseUnitId) — it keeps
        // only a display-friendly total: the Unidad Definida's unit, plus the TOTAL factor from
        // the purchase unit down to it (product of every level's QuantityInParentUnit). This
        // degrades gracefully for any chain depth (1, 2, 3+ levels) without OrderLine needing to
        // know how many levels exist.
        var packagingLevels = (await connection.QueryAsync<ArticlePackagingLevel>(
            "sp_ArticlePackagingLevel_GetByArticleId", new { ArticleId = article.ArticleId }, commandType: CommandType.StoredProcedure)).ToList();
        var definedLevel = packagingLevels.FirstOrDefault(l => l.IsDefinedUnit);
        var totalContentQuantity = packagingLevels.Aggregate(1m, (total, level) => total * level.QuantityInParentUnit);

        // Zone delivery-coverage gate — keyed off the ORDER'S OWN warehouse (not its
        // organization — a single Organization can have warehouses in different zones, and the
        // Warehouse is what actually receives the delivery; see CLAUDE.md's "Delivery Zones"
        // note), never the acting/impersonating user's. Null WarehouseZoneId ("not yet
        // assigned") or the warehouse's organization not being ASSOCIATE-type means "not
        // enforced yet" — never a block. Day-of-week is deliberately not considered — coverage
        // on any day is enough to be available for ordering.
        var coverage = await connection.QueryFirstOrDefaultAsync<SupplierDeliveryZoneCoverage>(
            "sp_SupplierDeliveryZone_CheckCoverage",
            new { SupplierId = article.SupplierId, WarehouseId = order.WarehouseId },
            commandType: CommandType.StoredProcedure);

        if (coverage is not null && coverage.EnforcementActive && !coverage.HasCoverage)
            throw new ApiException(ErrorCodes.ArticleSupplierZoneNotCovered,
                "This supplier does not deliver to the order's zone.", 409);

        // Resolve the current price for the Order's organization — same resolution the
        // ArticlePrices "current price" read path already uses (contract-over-global, with
        // the currency-hierarchy fallback baked into the SP itself). Exact param shape mirrors
        // ArticlePriceService.GetCurrentAsync's own call to this SP.
        var priceParams = new DynamicParameters();
        priceParams.Add("@ArticleId", article.ArticleId);
        priceParams.Add("@OrganizationId", order.OrganizationId);
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
            // SERVICE/MIXED suppliers are not required to carry a catalog ArticlePrice —
            // their price is negotiated at order time instead (see CLAUDE.md's "Supplier
            // type" section). PRODUCT suppliers keep the hard failure: a missing price
            // there is a real catalog data gap, not something to silently paper over.
            var isServiceOrMixed = article.SupplierType is SupplierType.Service or SupplierType.Mixed;
            if (!isServiceOrMixed)
            {
                if (resolvedCurrencyCode is null)
                    throw new ApiException(ErrorCodes.ArticlePriceCurrencyRequired, "A currency code could not be determined for this organization.", 400);

                throw new ApiException(ErrorCodes.ArticlePriceNotFound, "No current price found for this article.", 404);
            }

            if (!manualUnitPrice.HasValue || manualUnitPrice.Value <= 0 || string.IsNullOrWhiteSpace(manualCurrencyCode))
                throw new ApiException(ErrorCodes.ArticlePriceManualRequired, "This article has no catalog price — provide a manual unit price and currency for this order line.", 400);

            var normalizedCurrencyCode = manualCurrencyCode.Trim().ToUpperInvariant();
            var currencyExists = await connection.ExecuteScalarAsync<bool>(
                "sp_Currency_ExistsByCode", new { Code = normalizedCurrencyCode }, commandType: CommandType.StoredProcedure);
            if (!currencyExists)
                throw new ApiException(ErrorCodes.ArticlePriceInvalidCurrency, "Invalid or inactive currency code.", 400);

            unitPrice = manualUnitPrice.Value;
            currencyCode = normalizedCurrencyCode;
        }

        // Snapshot the Order's own organization's effective classification (own row, else
        // inherited from the nearest Super Asociado ancestor) — frozen onto the OrderLine as
        // plain CategoryId/CategoryCode/SubCategoryId/SubCategoryCode, never re-resolved live.
        // This is what protects historical spend-by-category reporting: a later Article
        // reclassification or Category Code rename must never retroactively change what an
        // already-placed Order reports. An unclassified article simply snapshots nulls —
        // classification is optional metadata, never a purchasing precondition.
        var classification = await connection.QueryFirstOrDefaultAsync<ArticleClassificationEffective>(
            "sp_ArticleClassification_GetEffectiveForArticle",
            new { ArticleId = article.ArticleId, OrganizationId = order.OrganizationId },
            commandType: CommandType.StoredProcedure);

        var linePararms = new DynamicParameters();
        linePararms.Add("@OrderLineToken", Guid.NewGuid());
        linePararms.Add("@OrderId", order.OrderId);
        linePararms.Add("@ArticleId", article.ArticleId);
        linePararms.Add("@Quantity", quantity);
        linePararms.Add("@PurchaseUnitId", article.PurchaseUnitId);
        linePararms.Add("@PurchaseQuantity", 1m);
        linePararms.Add("@ContentUnitId", definedLevel?.UnitOfMeasureId ?? article.PurchaseUnitId);
        linePararms.Add("@ContentQuantity", totalContentQuantity);
        linePararms.Add("@UnitPrice", unitPrice);
        linePararms.Add("@CurrencyCode", currencyCode);
        linePararms.Add("@CategoryId", classification?.CategoryId);
        linePararms.Add("@CategoryCode", classification?.CategoryCode);
        linePararms.Add("@SubCategoryId", classification?.SubCategoryId);
        linePararms.Add("@SubCategoryCode", classification?.SubCategoryCode);
        linePararms.Add("@Notes", (string?)null);
        linePararms.Add("@CreatedBy", context.ActorUserToken.ToString());

        var line = await connection.QueryFirstOrDefaultAsync<OrderLine>(
            "sp_OrderLine_Upsert", linePararms, commandType: CommandType.StoredProcedure);

        return line is null ? null : mapper.Map<OrderLineDto>(line);
    }

    public async Task<OrderLineDto?> EditLineAsync(Guid orderLineToken, decimal quantity, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existingLine = await GetLineByTokenAsync(connection, orderLineToken);
        if (existingLine is null)
            return null;

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = existingLine.OrderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot modify an order outside your organization.", 403);

        if (order.Status != OrderStatus.Draft)
            throw new ApiException(ErrorCodes.OrderNotDraft, "Only a draft order can be modified.", 409);

        var updated = await connection.QueryFirstOrDefaultAsync<OrderLine>(
            "sp_OrderLine_Edit",
            new { OrderLineToken = orderLineToken, Quantity = quantity, LastUpdatedBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<OrderLineDto>(updated);
    }

    public async Task<bool> DeleteLineAsync(Guid orderLineToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existingLine = await GetLineByTokenAsync(connection, orderLineToken);
        if (existingLine is null)
            return false;

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = existingLine.OrderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            return false;

        if (!await CanManageOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot modify an order outside your organization.", 403);

        if (order.Status != OrderStatus.Draft)
            throw new ApiException(ErrorCodes.OrderNotDraft, "Only a draft order can be modified.", 409);

        await connection.ExecuteAsync(
            "sp_OrderLine_Delete", new { OrderLineToken = orderLineToken }, commandType: CommandType.StoredProcedure);

        return true;
    }

    private static Task<OrderLine?> GetLineByTokenAsync(IDbConnection connection, Guid orderLineToken)
    {
        return connection.QueryFirstOrDefaultAsync<OrderLine>(
            "sp_OrderLine_GetByToken", new { OrderLineToken = orderLineToken }, commandType: CommandType.StoredProcedure)!;
    }

    public async Task<OrderDto?> SubmitAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot submit an order outside your organization.", 403);

        // Self-healing retry path: if every required approval step already ended up APPROVED
        // but the auto-completion inside ApproveOrderApprovalStepAsync failed transiently (e.g.
        // a SQL timeout) after the last step was already persisted as APPROVED, the order would
        // otherwise be stuck in PENDING_APPROVAL forever — sp_OrderApprovalStep_Approve refuses
        // to re-approve a non-PENDING step, so there'd be no way back in. Re-submitting picks up
        // exactly where the failed auto-completion left off.
        if (order.Status == OrderStatus.Pending_Approval)
        {
            var existingSteps = await GetApprovalStepsAsync(connection, order.OrderId);
            if (existingSteps.Count > 0 && existingSteps.All(s => s.Status == OrderApprovalStepStatus.Approved))
            {
                var approvedLines = await GetLinesAsync(connection, order.OrderId);
                return await CompleteSubmissionAsync(connection, order, approvedLines, context, cancellationToken);
            }

            throw new ApiException(ErrorCodes.OrderNotDraft, "This order is still pending approval.", 409);
        }

        if (order.Status != OrderStatus.Draft)
            throw new ApiException(ErrorCodes.OrderNotDraft, "Only a draft order can be submitted.", 409);

        var lines = await GetLinesAsync(connection, order.OrderId);
        if (lines.Count == 0)
            throw new ApiException(ErrorCodes.OrderEmpty, "Cannot submit an order with no lines.", 400);

        // Pure read, no transaction needed — if any configured Family threshold is crossed,
        // the Order goes to PENDING_APPROVAL instead of splitting into PurchaseOrders. See
        // CLAUDE.md's "Order Approval Workflow" section.
        var triggeredSteps = await EvaluateApprovalRequirementAsync(connection, order, lines);
        if (triggeredSteps.Count > 0)
            return await CreatePendingApprovalAsync(connection, order, lines, triggeredSteps, context, cancellationToken);

        return await CompleteSubmissionAsync(connection, order, lines, context, cancellationToken);
    }

    // Extracted from SubmitAsync so the same PO-split-and-SUBMIT logic can also be invoked from
    // ApproveOrderApprovalStepAsync once every required step is APPROVED (auto-completion, no
    // second manual Submit click needed).
    private async Task<OrderDto?> CompleteSubmissionAsync(DbConnection connection, Order order, List<OrderLine> lines, IRequestContext context, CancellationToken cancellationToken)
    {
        var actor = context.ActorUserToken.ToString();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Retained here (not re-grouped after commit) so the post-commit PDF/email block below
        // can send one email per supplier without a second GroupBy pass.
        var purchaseOrdersWithLines = new List<(PurchaseOrder PurchaseOrder, List<OrderLine> Lines)>();

        try
        {
            foreach (var supplierGroup in lines.GroupBy(l => l.SupplierId))
            {
                var poParams = new DynamicParameters();
                poParams.Add("@PurchaseOrderToken", Guid.NewGuid());
                poParams.Add("@OrderId", order.OrderId);
                poParams.Add("@SupplierId", supplierGroup.Key);
                poParams.Add("@OrganizationId", order.OrganizationId);
                poParams.Add("@WarehouseId", order.WarehouseId);
                poParams.Add("@CreatedBy", actor);

                var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
                    "sp_PurchaseOrder_Create", poParams, transaction, commandType: CommandType.StoredProcedure);

                if (purchaseOrder is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                var supplierLines = supplierGroup.ToList();
                purchaseOrdersWithLines.Add((purchaseOrder, supplierLines));

                // One PurchaseOrderLine per OrderLine in this supplier's group — an
                // independent snapshot copy captured at split time, not a shared row
                // with OrderLine (see .claude/OrdersModule.md for why).
                foreach (var line in supplierLines)
                {
                    var polParams = new DynamicParameters();
                    polParams.Add("@PurchaseOrderLineToken", Guid.NewGuid());
                    polParams.Add("@PurchaseOrderId", purchaseOrder.PurchaseOrderId);
                    polParams.Add("@OrderLineId", line.OrderLineId);
                    polParams.Add("@ArticleId", line.ArticleId);
                    polParams.Add("@Quantity", line.Quantity);
                    polParams.Add("@PurchaseUnitId", line.PurchaseUnitId);
                    polParams.Add("@PurchaseQuantity", line.PurchaseQuantity);
                    polParams.Add("@ContentUnitId", line.ContentUnitId);
                    polParams.Add("@ContentQuantity", line.ContentQuantity);
                    polParams.Add("@UnitPrice", line.UnitPrice);
                    polParams.Add("@CurrencyCode", line.CurrencyCode);
                    polParams.Add("@CategoryId", line.CategoryId);
                    polParams.Add("@CategoryCode", line.CategoryCode);
                    polParams.Add("@SubCategoryId", line.SubCategoryId);
                    polParams.Add("@SubCategoryCode", line.SubCategoryCode);
                    polParams.Add("@Notes", line.Notes);
                    polParams.Add("@CreatedBy", actor);

                    await connection.ExecuteAsync(
                        "sp_PurchaseOrderLine_Create", polParams, transaction, commandType: CommandType.StoredProcedure);
                }
            }

            var updatedOrder = await connection.QueryFirstOrDefaultAsync<Order>(
                "sp_Order_SetStatus",
                new { OrderToken = order.OrderToken, Status = OrderStatusCodes.Submitted, ActorBy = actor },
                transaction,
                commandType: CommandType.StoredProcedure);

            await transaction.CommitAsync(cancellationToken);

            if (updatedOrder is null)
                return null;

            var dto = mapper.Map<OrderDto>(updatedOrder);
            dto.Lines = mapper.MapList<OrderLineDto>(lines);
            dto.LineCount = dto.Lines.Count;
            dto.ApprovalSteps = mapper.MapList<OrderApprovalStepDto>(await GetApprovalStepsAsync(connection, order.OrderId));

            // Best-effort, non-blocking: the Order/PurchaseOrders are already committed above, so
            // a QuestPDF bug or an SMTP outage here must never fail an already-successful
            // confirmation. dto.PdfUrl simply stays null if generation fails.
            await SendOrderConfirmationAsync(connection, updatedOrder, lines, purchaseOrdersWithLines, context, dto, cancellationToken);

            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task SendOrderConfirmationAsync(
        DbConnection connection,
        Order updatedOrder,
        List<OrderLine> lines,
        List<(PurchaseOrder PurchaseOrder, List<OrderLine> Lines)> purchaseOrdersWithLines,
        IRequestContext context,
        OrderDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var (organizationName, organizationLanguageCode) = await GetOrganizationNameAndLanguageAsync(connection, updatedOrder.OrganizationToken);
            var buyerEmail = await GetUserEmailAsync(connection, context.ActorUserToken);
            var warehouseInfo = await GetWarehouseHeaderInfoAsync(connection, updatedOrder.WarehouseToken);

            var fullPdfBytes = OrderConfirmationDocument.BuildFullOrderPdf(updatedOrder, organizationName, lines, warehouseInfo, organizationLanguageCode);
            await orderPdfStorage.SaveAsync(updatedOrder.OrderToken, fullPdfBytes, cancellationToken);

            var pdfUrl = $"/orders/{updatedOrder.OrderToken}/downloadPdf";
            await connection.ExecuteAsync(
                "sp_Order_SetPdfUrl",
                new { updatedOrder.OrderToken, PdfUrl = pdfUrl, LastUpdatedUtc = DateTime.UtcNow, LastUpdatedBy = context.ActorUserToken.ToString() },
                commandType: CommandType.StoredProcedure);
            dto.PdfUrl = pdfUrl;

            if (!string.IsNullOrWhiteSpace(buyerEmail))
            {
                await emailSender.SendAsync(new EmailMessage
                {
                    ToAddress = buyerEmail,
                    Subject = $"{OrderConfirmationLocalization.Label("OrderConfirmedHeading", organizationLanguageCode)} — {organizationName}",
                    HtmlBody = OrderConfirmationEmailContent.BuildBuyerEmailHtml(updatedOrder, organizationName, lines, warehouseInfo, organizationLanguageCode),
                    Attachments = [new EmailAttachment { FileName = "order-confirmation.pdf", Content = fullPdfBytes }]
                }, cancellationToken);
            }

            foreach (var (purchaseOrder, supplierLines) in purchaseOrdersWithLines)
            {
                if (string.IsNullOrWhiteSpace(purchaseOrder.SupplierEmail))
                    continue;

                var supplierPdf = OrderConfirmationDocument.BuildSupplierPdf(updatedOrder, organizationName, purchaseOrder.SupplierName ?? "Supplier", purchaseOrder.PurchaseOrderNumber, supplierLines, warehouseInfo, purchaseOrder.SupplierLanguageCode);
                await emailSender.SendAsync(new EmailMessage
                {
                    ToAddress = purchaseOrder.SupplierEmail,
                    Subject = $"{OrderConfirmationLocalization.Label("NewPurchaseOrderHeading", purchaseOrder.SupplierLanguageCode)} — {organizationName}",
                    HtmlBody = OrderConfirmationEmailContent.BuildSupplierEmailHtml(updatedOrder, organizationName, purchaseOrder.SupplierName ?? "Supplier", purchaseOrder.PurchaseOrderNumber, supplierLines, warehouseInfo, purchaseOrder.SupplierLanguageCode),
                    Attachments = [new EmailAttachment { FileName = "purchase-order.pdf", Content = supplierPdf }]
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Order confirmation PDF/email generation failed for order {OrderToken}", updatedOrder.OrderToken);
        }
    }

    private sealed class TriggeredApprovalStep
    {
        public int FamilyId { get; set; }
        public string FamilyCode { get; set; } = default!;
        public int Level { get; set; }
        public decimal ThresholdAmount { get; set; }
        public decimal ActualFamilyAmount { get; set; }
        public string CurrencyCode { get; set; } = default!;
        public int ApproverUserId { get; set; }
    }

    private sealed class ArticleFamilyProjection
    {
        public int ArticleId { get; set; }
        public int? FamilyId { get; set; }
    }

    // Evaluates this Order's own total per Family (never cumulative history — confirmed with
    // the user) against that Family's configured FamilyApprovalThresholds for the Order's
    // organization. Only lines whose CurrencyCode matches the organization's own resolved
    // currency count toward a Family's total — a deliberate simplification, not a bug: this
    // codebase doesn't do FX conversion anywhere else either. Sequential/cumulative levels: if
    // the highest crossed Level is N, every level 1..N is returned (all of them need to sign
    // off, in order — see OrderApprovalStep's "your turn" gate in Approve/Reject below).
    private async Task<List<TriggeredApprovalStep>> EvaluateApprovalRequirementAsync(IDbConnection connection, Order order, List<OrderLine> lines)
    {
        var result = new List<TriggeredApprovalStep>();

        var articleIds = lines.Select(l => l.ArticleId).Distinct().ToList();
        var articleFamilies = (await connection.QueryAsync<ArticleFamilyProjection>(
            "sp_Article_GetFamilyIdsByArticleIds", new { ArticleIds = string.Join(',', articleIds) }, commandType: CommandType.StoredProcedure))
            .ToDictionary(a => a.ArticleId, a => a.FamilyId);

        var currencyParams = new DynamicParameters();
        currencyParams.Add("@OrganizationId", order.OrganizationId);
        currencyParams.Add("@CurrencyCode", null, DbType.AnsiString, size: 10, direction: ParameterDirection.InputOutput);
        await connection.ExecuteAsync("sp_Organization_ResolveCurrencyCode", currencyParams, commandType: CommandType.StoredProcedure);
        var orgCurrencyCode = currencyParams.Get<string?>("@CurrencyCode");

        if (orgCurrencyCode is null)
            return result;

        var familyTotals = lines
            .Where(l => articleFamilies.TryGetValue(l.ArticleId, out var familyId) && familyId.HasValue && l.CurrencyCode == orgCurrencyCode)
            .GroupBy(l => articleFamilies[l.ArticleId]!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity * l.UnitPrice));

        foreach (var (familyId, total) in familyTotals)
        {
            var levels = (await connection.QueryAsync<FamilyApprovalThreshold>(
                "sp_FamilyApprovalThreshold_GetPaged",
                new { OrganizationId = order.OrganizationId, PageNumber = 1, PageSize = MaxPageSize, FamilyId = familyId, IncludeInactive = false },
                commandType: CommandType.StoredProcedure))
                .OrderBy(t => t.Level)
                .ToList();

            var highestTriggeredLevel = levels.Where(t => total >= t.ThresholdAmount).Select(t => (int?)t.Level).DefaultIfEmpty().Max();
            if (!highestTriggeredLevel.HasValue)
                continue;

            foreach (var level in levels.Where(t => t.Level <= highestTriggeredLevel.Value))
            {
                result.Add(new TriggeredApprovalStep
                {
                    FamilyId = familyId,
                    FamilyCode = level.FamilyCode,
                    Level = level.Level,
                    ThresholdAmount = level.ThresholdAmount,
                    ActualFamilyAmount = total,
                    CurrencyCode = orgCurrencyCode,
                    ApproverUserId = level.ApproverUserId
                });
            }
        }

        return result;
    }

    private async Task<OrderDto?> CreatePendingApprovalAsync(IDbConnection connection, Order order, List<OrderLine> lines, List<TriggeredApprovalStep> triggeredSteps, IRequestContext context, CancellationToken cancellationToken)
    {
        var actor = context.ActorUserToken.ToString();

        foreach (var step in triggeredSteps)
        {
            var stepParams = new DynamicParameters();
            stepParams.Add("@OrderApprovalStepToken", Guid.NewGuid());
            stepParams.Add("@OrderId", order.OrderId);
            stepParams.Add("@FamilyId", step.FamilyId);
            stepParams.Add("@FamilyCode", step.FamilyCode);
            stepParams.Add("@Level", step.Level);
            stepParams.Add("@ThresholdAmount", step.ThresholdAmount);
            stepParams.Add("@ActualFamilyAmount", step.ActualFamilyAmount);
            stepParams.Add("@CurrencyCode", step.CurrencyCode);
            stepParams.Add("@ApproverUserId", step.ApproverUserId);
            stepParams.Add("@CreatedBy", actor);
            await connection.ExecuteAsync("sp_OrderApprovalStep_Create", stepParams, commandType: CommandType.StoredProcedure);
        }

        var pendingOrder = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_SetStatus",
            new { OrderToken = order.OrderToken, Status = OrderStatusCodes.PendingApproval, ActorBy = actor },
            commandType: CommandType.StoredProcedure);

        if (pendingOrder is null)
            return null;

        var allSteps = await GetApprovalStepsAsync(connection, order.OrderId);

        var dto = mapper.Map<OrderDto>(pendingOrder);
        dto.Lines = mapper.MapList<OrderLineDto>(lines);
        dto.LineCount = dto.Lines.Count;
        dto.ApprovalSteps = mapper.MapList<OrderApprovalStepDto>(allSteps);

        // Email only the immediately-actionable step per triggered Family — Level 1 is always
        // present whenever any level triggers (EvaluateApprovalRequirementAsync returns every
        // level up to the highest crossed), so a fresh PENDING Level-1 row is always "your
        // turn" right now. Filtering on Status == Pending (not just Level == 1) matters
        // because a rejected-then-resubmitted order can have older terminal rows at the same
        // Level for the same Family (see .claude/OrderApprovalModule.md's "no unique
        // constraint" note) — Best-effort/non-blocking, same convention as
        // SendOrderConfirmationAsync.
        var newlyActionableSteps = allSteps.Where(s => s.Level == 1 && s.Status == OrderApprovalStepStatus.Pending).ToList();
        if (newlyActionableSteps.Count > 0)
        {
            var (organizationName, organizationLanguageCode) = await GetOrganizationNameAndLanguageAsync(connection, order.OrganizationToken);
            foreach (var step in newlyActionableSteps)
                await SendApprovalRequestEmailAsync(connection, step, order, organizationName, organizationLanguageCode, cancellationToken);
        }

        return dto;
    }

    // Shared by the authenticated Approve path and the anonymous email-token Approve path —
    // calls sp_OrderApprovalStep_Approve, then either auto-completes the submission (every
    // step now APPROVED) or emails the next-level sibling for the SAME Family, if one is now
    // unblocked. `context` stamps CreatedBy/LastUpdatedBy on any resulting PurchaseOrder rows
    // and the buyer confirmation email — the anonymous caller passes a synthetic context built
    // from the approver's own frozen identity (see EmailApprovalRequestContext below), so the
    // audit trail reads identically to an in-app approval.
    private async Task<OrderApprovalStep> ApproveStepAndAdvanceAsync(DbConnection connection, OrderApprovalStep step, Order order, IRequestContext context, CancellationToken cancellationToken)
    {
        var approved = await connection.QueryFirstOrDefaultAsync<OrderApprovalStep>(
            "sp_OrderApprovalStep_Approve",
            new { OrderApprovalStepToken = step.OrderApprovalStepToken, DecidedBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        if (approved is null)
            throw new ApiException(ErrorCodes.OrderApprovalStepAlreadyDecided, "This approval step was already decided.", 409);

        // A rectification-triggered step never participates in the Order's own submission
        // auto-complete below — it's scoped to its own batch (TriggeringPurchaseOrderRectificationId),
        // never the Order's full approval history, which can include unrelated, already-terminal
        // steps from the original Submit or an earlier rectification. See
        // .claude/PurchaseOrderRectificationModule.md.
        if (approved.TriggeringPurchaseOrderRectificationId.HasValue)
        {
            var rectificationSteps = (await GetApprovalStepsAsync(connection, order.OrderId))
                .Where(s => s.TriggeringPurchaseOrderRectificationId == approved.TriggeringPurchaseOrderRectificationId)
                .ToList();

            if (rectificationSteps.Count > 0 && rectificationSteps.All(s => s.Status == OrderApprovalStepStatus.Approved))
            {
                await connection.ExecuteAsync(
                    "sp_PurchaseOrderRectification_SetStatus",
                    new { PurchaseOrderRectificationId = approved.TriggeringPurchaseOrderRectificationId.Value, Status = PurchaseOrderRectificationStatusCodes.Applied },
                    commandType: CommandType.StoredProcedure);
            }

            return approved;
        }

        // Auto-complete the submission the moment every required step for this Order is
        // APPROVED — confirmed with the user, no second manual Submit click.
        var allSteps = await GetApprovalStepsAsync(connection, order.OrderId);
        if (allSteps.All(s => s.Status == OrderApprovalStepStatus.Approved))
        {
            var lines = await GetLinesAsync(connection, order.OrderId);
            await CompleteSubmissionAsync(connection, order, lines, context, cancellationToken);
        }
        else
        {
            var nextStep = allSteps.FirstOrDefault(s => s.FamilyId == approved.FamilyId && s.Level == approved.Level + 1 && s.Status == OrderApprovalStepStatus.Pending);
            if (nextStep is not null)
            {
                var (organizationName, organizationLanguageCode) = await GetOrganizationNameAndLanguageAsync(connection, order.OrganizationToken);
                await SendApprovalRequestEmailAsync(connection, nextStep, order, organizationName, organizationLanguageCode, cancellationToken);
            }
        }

        return approved;
    }

    public async Task<OrderApprovalStepDto?> ApproveOrderApprovalStepAsync(Guid stepToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var step = await GetApprovalStepByTokenAsync(connection, stepToken);
        if (step is null)
            return null;

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = step.OrderToken }, commandType: CommandType.StoredProcedure);
        if (order is null)
            return null;

        await EnsureCanDecideStepAsync(connection, context, step);

        var approved = await ApproveStepAndAdvanceAsync(connection, step, order, context, cancellationToken);

        return mapper.Map<OrderApprovalStepDto>(approved);
    }

    // Anonymous single-use email-approval link — see .claude/OrderApprovalModule.md. The token
    // itself is the entire authorization; every failure mode gets its own specific
    // ApiException/ErrorCode so the confirmation page can show a precise, friendly message
    // instead of a generic error.
    public async Task<OrderApprovalEmailPreviewDto> GetApprovalStepPreviewByEmailTokenAsync(Guid emailToken, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var step = await connection.QueryFirstOrDefaultAsync<OrderApprovalStep>(
            "sp_OrderApprovalStep_GetByEmailToken", new { EmailApprovalToken = emailToken }, commandType: CommandType.StoredProcedure);

        if (step is null)
            throw new ApiException(ErrorCodes.OrderApprovalEmailTokenNotFound, "This approval link is not valid.", 404);

        // Expired/AlreadyUsed/AlreadyDecided are normal, informative outcomes for a one-click
        // link — not application errors — so they're a Status field on a 200 response, not a
        // FailureResponse (unlike the mutating Approve call below, where they are).
        var status =
            step.EmailApprovalTokenUsedUtc is not null ? "AlreadyUsed" :
            step.EmailApprovalTokenExpiresUtc is null || step.EmailApprovalTokenExpiresUtc < DateTime.UtcNow ? "Expired" :
            step.Status != OrderApprovalStepStatus.Pending ? "AlreadyDecided" :
            "Ready";

        return new OrderApprovalEmailPreviewDto
        {
            Status = status,
            OrganizationName = step.OrganizationName ?? "—",
            WarehouseName = step.WarehouseName ?? "—",
            FamilyCode = step.FamilyCode,
            Level = step.Level,
            ThresholdAmount = step.ThresholdAmount,
            ActualFamilyAmount = step.ActualFamilyAmount,
            CurrencyCode = step.CurrencyCode,
            OrderReference = step.OrderToken.ToString()[..8].ToUpperInvariant()
        };
    }

    public async Task<OrderApprovalEmailApproveResultDto> ApproveOrderApprovalStepByEmailTokenAsync(Guid emailToken, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var step = await connection.QueryFirstOrDefaultAsync<OrderApprovalStep>(
            "sp_OrderApprovalStep_GetByEmailToken", new { EmailApprovalToken = emailToken }, commandType: CommandType.StoredProcedure);

        if (step is null)
            throw new ApiException(ErrorCodes.OrderApprovalEmailTokenNotFound, "This approval link is not valid.", 404);

        if (step.EmailApprovalTokenUsedUtc is not null)
            throw new ApiException(ErrorCodes.OrderApprovalEmailTokenAlreadyUsed, "This approval link has already been used.", 409);

        if (step.EmailApprovalTokenExpiresUtc is null || step.EmailApprovalTokenExpiresUtc < DateTime.UtcNow)
            throw new ApiException(ErrorCodes.OrderApprovalEmailTokenExpired, "This approval link has expired. Please sign in to the system instead.", 410);

        if (step.Status != OrderApprovalStepStatus.Pending)
            throw new ApiException(ErrorCodes.OrderApprovalEmailTokenStepAlreadyDecided, "This approval request has already been resolved.", 409);

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = step.OrderToken }, commandType: CommandType.StoredProcedure);

        if (order is null || order.Status != OrderStatus.Pending_Approval)
            throw new ApiException(ErrorCodes.OrderApprovalEmailTokenStepAlreadyDecided, "This order is no longer awaiting approval.", 409);

        // Same "your turn" gate EnsureCanDecideStepAsync applies for the authenticated path —
        // reimplemented here since there's no IRequestContext-based caller to authorize; the
        // token itself already proves who's deciding.
        var siblingSteps = await GetApprovalStepsAsync(connection, step.OrderId);
        if (siblingSteps.Any(s => s.FamilyId == step.FamilyId && s.Level < step.Level && s.Status != OrderApprovalStepStatus.Approved))
            throw new ApiException(ErrorCodes.OrderApprovalEmailTokenPriorLevelPending, "An earlier level for this Family must be approved first.", 409);

        var anonymousContext = new EmailApprovalRequestContext(step.ApproverUserToken);
        var approved = await ApproveStepAndAdvanceAsync(connection, step, order, anonymousContext, cancellationToken);

        await connection.ExecuteAsync(
            "sp_OrderApprovalStep_MarkEmailTokenUsed", new { OrderApprovalStepToken = step.OrderApprovalStepToken }, commandType: CommandType.StoredProcedure);

        var allStepsAfter = await GetApprovalStepsAsync(connection, step.OrderId);

        return new OrderApprovalEmailApproveResultDto
        {
            FamilyCode = approved.FamilyCode,
            Level = approved.Level,
            OrderFullyApproved = allStepsAfter.All(s => s.Status == OrderApprovalStepStatus.Approved)
        };
    }

    // Minimal IRequestContext for the anonymous email-token approval path — there is no HTTP
    // session/JWT to read claims from, but there IS a real, known identity: the approver the
    // step was frozen to. Using their own UserToken as ActorUserToken/EffectiveUserToken means
    // CreatedBy/LastUpdatedBy on any resulting PurchaseOrder rows (and the buyer confirmation
    // email) read identically to an in-app approval. RoleLevel stays 0 since nothing downstream
    // in this call chain needs role-based authorization — the email token itself already was
    // the authorization.
    private sealed class EmailApprovalRequestContext(Guid approverUserToken) : IRequestContext
    {
        public Guid ActorUserToken => approverUserToken;
        public Guid EffectiveUserToken => approverUserToken;
        public int? OrganizationId => null;
        public string? OrganizationTypeCode => null;
        public int? SupplierId => null;
        public int RoleLevel => 0;
        public int ActorRoleLevel => 0;
        public int? ActorOrganizationId => null;
        public bool IsAuthenticated => false;
        public bool IsImpersonating => false;
    }

    // Sends the "your turn to approve" email for a single OrderApprovalStep — issues a fresh
    // single-use anonymous token (7-day expiry, confirmed with the user) via
    // sp_OrderApprovalStep_IssueEmailToken, then builds and sends the HTML via
    // OrderApprovalEmailContent. Best-effort/non-blocking, same convention as
    // SendOrderConfirmationAsync — an SMTP outage here must never fail the Submit/Approve call
    // that triggered it.
    private async Task SendApprovalRequestEmailAsync(IDbConnection connection, OrderApprovalStep step, Order order, string organizationName, string? organizationLanguageCode, CancellationToken cancellationToken)
    {
        try
        {
            var approverEmail = await GetUserEmailAsync(connection, step.ApproverUserToken);
            if (string.IsNullOrWhiteSpace(approverEmail))
                return;

            var emailToken = Guid.NewGuid();
            var expiresUtc = DateTime.UtcNow.AddDays(7);

            await connection.ExecuteAsync(
                "sp_OrderApprovalStep_IssueEmailToken",
                new { OrderApprovalStepToken = step.OrderApprovalStepToken, EmailApprovalToken = emailToken, ExpiresUtc = expiresUtc },
                commandType: CommandType.StoredProcedure);

            var frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
            var approvalLink = $"{frontendBaseUrl}/approve-order/{emailToken}";

            await emailSender.SendAsync(new EmailMessage
            {
                ToAddress = approverEmail,
                Subject = $"{OrderApprovalEmailLocalization.Label("ApprovalNeededHeading", organizationLanguageCode)} — {step.FamilyCode}",
                HtmlBody = OrderApprovalEmailContent.BuildApprovalRequestEmailHtml(order, organizationName, step, approvalLink, frontendBaseUrl, organizationLanguageCode)
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Approval-request email failed for OrderApprovalStep {OrderApprovalStepToken}", step.OrderApprovalStepToken);
        }
    }

    public async Task<OrderApprovalStepDto?> RejectOrderApprovalStepAsync(Guid stepToken, string reason, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var step = await GetApprovalStepByTokenAsync(connection, stepToken);
        if (step is null)
            return null;

        await EnsureCanDecideStepAsync(connection, context, step);

        // A rectification-triggered step is rejected independently of the Order itself — the
        // Order stays exactly as it is (already SUBMITTED); only the proposed correction is
        // discarded. Uses a dedicated SP so the sibling-cancel is scoped to this rectification's
        // own batch, never every pending step for the whole OrderId (two different
        // PurchaseOrders split from the same Order can each have their own rectification pending
        // approval at once). See .claude/PurchaseOrderRectificationModule.md.
        if (step.TriggeringPurchaseOrderRectificationId.HasValue)
        {
            var rejectedRectificationStep = await connection.QueryFirstOrDefaultAsync<OrderApprovalStep>(
                "sp_OrderApprovalStep_RejectRectificationStep",
                new { OrderApprovalStepToken = stepToken, RejectionReason = reason, DecidedBy = context.ActorUserToken.ToString() },
                commandType: CommandType.StoredProcedure);

            if (rejectedRectificationStep is null)
                throw new ApiException(ErrorCodes.OrderApprovalStepAlreadyDecided, "This approval step was already decided.", 409);

            await connection.ExecuteAsync(
                "sp_PurchaseOrderRectification_SetStatus",
                new { PurchaseOrderRectificationId = step.TriggeringPurchaseOrderRectificationId.Value, Status = PurchaseOrderRectificationStatusCodes.Rejected },
                commandType: CommandType.StoredProcedure);

            return mapper.Map<OrderApprovalStepDto>(rejectedRectificationStep);
        }

        var rejected = await connection.QueryFirstOrDefaultAsync<OrderApprovalStep>(
            "sp_OrderApprovalStep_Reject",
            new { OrderApprovalStepToken = stepToken, RejectionReason = reason, DecidedBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        if (rejected is null)
            throw new ApiException(ErrorCodes.OrderApprovalStepAlreadyDecided, "This approval step was already decided.", 409);

        await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_SetStatus",
            new { OrderToken = step.OrderToken, Status = OrderStatusCodes.Draft, ActorBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        return mapper.Map<OrderApprovalStepDto>(rejected);
    }

    public async Task<PagedResult<OrderApprovalStepDto>> GetPendingApprovalStepsAsync(int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        var user = await connection.QueryFirstOrDefaultAsync<User>(
            "sp_User_GetByToken", new { UserToken = context.EffectiveUserToken }, commandType: CommandType.StoredProcedure);
        if (user is null)
            return new PagedResult<OrderApprovalStepDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

        var p = new DynamicParameters();
        p.Add("@ApproverUserId", user.UserId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        var rows = (await connection.QueryAsync<OrderApprovalStepPageRow>(
            "sp_OrderApprovalStep_GetPendingForApprover", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<OrderApprovalStepDto>
        {
            Items = mapper.MapList<OrderApprovalStepDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    private sealed class OrderApprovalStepPageRow : OrderApprovalStep { public int TotalCount { get; set; } }

    private static Task<OrderApprovalStep?> GetApprovalStepByTokenAsync(IDbConnection connection, Guid stepToken)
    {
        return connection.QueryFirstOrDefaultAsync<OrderApprovalStep?>(
            "sp_OrderApprovalStep_GetByToken", new { OrderApprovalStepToken = stepToken }, commandType: CommandType.StoredProcedure);
    }

    // The step's frozen ApproverUserId must match the caller's own resolved UserId, or the
    // caller is SuperAdmin (same escape-hatch convention used throughout this codebase). The
    // "your turn" gate — no lower, still-non-APPROVED sibling Level for the same
    // Order+Family — is enforced here too, not just left to sp_OrderApprovalStep_Approve/Reject's
    // lighter existing-status guard.
    private async Task EnsureCanDecideStepAsync(IDbConnection connection, IRequestContext context, OrderApprovalStep step)
    {
        if (context.RoleLevel < SuperAdminRoleLevel)
        {
            var user = await connection.QueryFirstOrDefaultAsync<User>(
                "sp_User_GetByToken", new { UserToken = context.EffectiveUserToken }, commandType: CommandType.StoredProcedure);

            if (user is null || user.UserId != step.ApproverUserId)
                throw new ApiException(ErrorCodes.OrderApprovalStepForbidden, "You are not the designated approver for this step.", 403);
        }

        var siblingSteps = await GetApprovalStepsAsync(connection, step.OrderId);
        var priorLevelPending = siblingSteps.Any(s => s.FamilyId == step.FamilyId && s.Level < step.Level && s.Status != OrderApprovalStepStatus.Approved);
        if (priorLevelPending)
            throw new ApiException(ErrorCodes.OrderApprovalStepPriorLevelPending, "An earlier level for this Family must be approved first.", 409);
    }

    public async Task<bool> DeleteAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            return false;

        if (!await CanManageOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot delete an order outside your organization.", 403);

        if (order.Status != OrderStatus.Draft)
            throw new ApiException(ErrorCodes.OrderNotDraft, "Only a draft order can be deleted.", 409);

        await connection.ExecuteAsync(
            "sp_Order_Delete", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        return true;
    }

    public async Task<OrderDto?> CancelAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot cancel an order outside your organization.", 403);

        // A submitter withdrawing a request stuck in PENDING_APPROVAL is a reasonable capability
        // — same as a rejection, this voids any still-PENDING steps first.
        if (order.Status is not (OrderStatus.Draft or OrderStatus.Pending_Approval))
            throw new ApiException(ErrorCodes.OrderNotCancellable, "Only a draft or pending-approval order can be cancelled.", 409);

        if (order.Status == OrderStatus.Pending_Approval)
            await connection.ExecuteAsync(
                "sp_OrderApprovalStep_CancelPendingForOrder",
                new { order.OrderId, DecidedBy = context.ActorUserToken.ToString() },
                commandType: CommandType.StoredProcedure);

        var updated = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_SetStatus",
            new { OrderToken = orderToken, Status = OrderStatusCodes.Cancelled, ActorBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<OrderDto>(updated);
    }

    // Creates a new Draft order for the same Warehouse as a SUBMITTED source order, re-adding
    // every line via the existing AddLineAsync. Passing the ORIGINAL line's frozen UnitPrice/
    // CurrencyCode as the "manual" fallback is deliberate: AddLineAsync always tries the live
    // catalog price first regardless, so a re-priceable article (PRODUCT, or a priced SERVICE/
    // MIXED) still gets today's current price; the fallback only engages for an unpriced
    // SERVICE/MIXED article that was originally added with a manual price — without it, that
    // line would 400 with ARTICLE_PRICE_MANUAL_REQUIRED on every copy. A line whose article can
    // no longer be added (inactive/deleted/superseded/no price) is skipped and reported, never
    // aborting the whole copy — same partial-failure convention as ImportLinesAsync.
    public async Task<CopyOrderResultDto> CopyAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var source = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        if (source is null)
            throw new ApiException(ErrorCodes.OrderNotFound, "Order not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, source.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot copy an order outside your organization.", 403);

        if (source.Status != OrderStatus.Submitted)
            throw new ApiException(ErrorCodes.OrderCopyInvalidSourceStatus, "Only a submitted order can be copied.", 409);

        var sourceLines = await GetLinesAsync(connection, source.OrderId);

        var createParams = new DynamicParameters();
        createParams.Add("@OrderToken", Guid.NewGuid());
        createParams.Add("@OrganizationId", source.OrganizationId);
        createParams.Add("@WarehouseId", source.WarehouseId);
        createParams.Add("@Notes", (string?)null);
        createParams.Add("@CreatedBy", context.ActorUserToken.ToString());

        var newOrder = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_Create", createParams, commandType: CommandType.StoredProcedure)
            ?? throw new InvalidOperationException("sp_Order_Create returned no row for a Warehouse/Organization already validated above.");

        var result = new CopyOrderResultDto { NewOrderToken = newOrder.OrderToken, TotalLines = sourceLines.Count };

        foreach (var line in sourceLines)
        {
            try
            {
                await AddLineAsync(newOrder.OrderToken, line.ArticleToken, line.Quantity, line.UnitPrice, line.CurrencyCode, context, cancellationToken);
                result.CopiedCount++;
            }
            catch (ApiException ex)
            {
                result.SkippedLines.Add(new CopyOrderSkippedLineDto { ArticleToken = line.ArticleToken, ArticleName = line.ArticleName, Code = ex.Code, Description = ex.Message });
            }
            catch (Exception)
            {
                result.SkippedLines.Add(new CopyOrderSkippedLineDto { ArticleToken = line.ArticleToken, ArticleName = line.ArticleName, Code = ErrorCodes.UnhandledError, Description = "An unexpected error occurred while copying this line." });
            }
        }

        result.SkippedCount = result.SkippedLines.Count;
        return result;
    }

    // Bulk-adds lines to an existing Draft order from an uploaded Excel file. Column layout
    // matches OrderTemplateService.ExportAsync's export exactly: SupplierName, SupplierSku,
    // ArticleName (informational only, never used to resolve), Quantity, Price, CurrencyCode,
    // ArticleToken (hidden, primary match). Row resolution mirrors
    // ArticleService.BulkImportArticlesAsync's precedence: ArticleToken first, falling back to
    // (SupplierName -> SupplierId, SupplierSku). Price/CurrencyCode are only actually honored by
    // AddLineAsync when the resolved article's SupplierType is SERVICE/MIXED and no catalog
    // price resolves — a PRODUCT article's price always comes from the live catalog regardless
    // of what's in the cell, so a client can never override real catalog truth. Unlike
    // ApplyOrderTemplateAsync's per-line handling, ArticlePriceManualRequired here IS a hard row
    // failure — there is no interactive modal possible mid-file-parse, so the Price/CurrencyCode
    // columns exist precisely so the user fills them in before uploading.
    public async Task<ImportOrderLinesResultDto> ImportLinesAsync(Guid orderToken, byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            throw new ApiException(ErrorCodes.OrderNotFound, "Order not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot modify an order outside your organization.", 403);

        if (order.Status != OrderStatus.Draft)
            throw new ApiException(ErrorCodes.OrderNotDraft, "Only a draft order can be modified.", 409);

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(fileBytes));
        }
        catch
        {
            throw new ApiException(ErrorCodes.OrderImportLinesInvalidFile, "The uploaded file is not a valid Excel (.xlsx) file.", 400);
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.First();

            var dataRows = worksheet.RowsUsed()
                .Skip(1)
                .Where(row => row.CellsUsed().Any(c => !string.IsNullOrWhiteSpace(c.GetString())))
                .ToList();

            if (dataRows.Count > MaxBulkImportRows)
                throw new ApiException(ErrorCodes.OrderImportLinesTooManyRows, $"A single import file cannot contain more than {MaxBulkImportRows} rows.", 400);

            var result = new ImportOrderLinesResultDto { TotalRows = dataRows.Count };

            if (dataRows.Count == 0)
                return result;

            var supplierCache = new Dictionary<string, Supplier?>(StringComparer.OrdinalIgnoreCase);

            // Rows are processed strictly sequentially — same convention as every other bulk
            // import in this codebase — one row's failure never aborts the rest.
            foreach (var row in dataRows)
            {
                var rowNumber = row.RowNumber();

                var supplierName = row.Cell(1).GetString().Trim();
                var supplierSku = row.Cell(2).GetString().Trim();
                var quantityText = row.Cell(4).GetString().Trim();
                var priceText = row.Cell(5).GetString().Trim();
                var currencyCodeText = row.Cell(6).GetString().Trim();
                var articleTokenText = row.Cell(7).GetString().Trim();

                var rowIdentifier = !string.IsNullOrWhiteSpace(supplierSku) ? supplierSku : (string.IsNullOrWhiteSpace(articleTokenText) ? null : articleTokenText);

                if (!decimal.TryParse(quantityText, out var quantity) || quantity <= 0)
                {
                    result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.OrderImportLinesRowInvalid, Description = "Quantity is required and must be greater than zero." });
                    continue;
                }

                Article? article;
                if (!string.IsNullOrWhiteSpace(articleTokenText))
                {
                    if (!Guid.TryParse(articleTokenText, out var articleToken))
                    {
                        result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.OrderImportLinesRowInvalid, Description = "ArticleToken is not a valid identifier." });
                        continue;
                    }

                    // Pass the Order's OWN organization, never the acting user's identity — see
                    // AddLineAsync's comment above for the full reasoning.
                    article = await connection.QueryFirstOrDefaultAsync<Article>(
                        "sp_Article_GetByToken", new { ArticleToken = articleToken, OrganizationId = order.OrganizationId, ContextRoleLevel = 0 }, commandType: CommandType.StoredProcedure);

                    if (article is null)
                    {
                        result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleNotFound, Description = $"No article found for ArticleToken '{articleTokenText}'." });
                        continue;
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(supplierName) || string.IsNullOrWhiteSpace(supplierSku))
                    {
                        result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.OrderImportLinesRowInvalid, Description = "SupplierName and SupplierSku are required when ArticleToken is blank." });
                        continue;
                    }

                    var supplierKey = supplierName.ToUpperInvariant();
                    if (!supplierCache.TryGetValue(supplierKey, out var supplier))
                    {
                        supplier = await connection.QueryFirstOrDefaultAsync<Supplier>(
                            "sp_Supplier_GetByNormalizedName", new { NormalizedName = supplierKey }, commandType: CommandType.StoredProcedure);
                        supplierCache[supplierKey] = supplier;
                    }

                    if (supplier is null)
                    {
                        result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.SupplierNotFound, Description = $"Supplier '{supplierName}' was not found." });
                        continue;
                    }

                    article = await connection.QueryFirstOrDefaultAsync<Article>(
                        "sp_Article_GetBySupplierSku", new { SupplierId = supplier.SupplierId, SupplierSku = supplierSku }, commandType: CommandType.StoredProcedure);

                    if (article is null)
                    {
                        result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleNotFound, Description = $"No article found for SupplierSku '{supplierSku}'." });
                        continue;
                    }
                }

                decimal? manualUnitPrice = null;
                if (!string.IsNullOrWhiteSpace(priceText) && decimal.TryParse(priceText, out var parsedPrice) && parsedPrice > 0)
                    manualUnitPrice = parsedPrice;
                var manualCurrencyCode = string.IsNullOrWhiteSpace(currencyCodeText) ? null : currencyCodeText.Trim();

                try
                {
                    await AddLineAsync(orderToken, article.ArticleToken, quantity, manualUnitPrice, manualCurrencyCode, context, cancellationToken);
                    result.SucceededCount++;
                }
                catch (ApiException ex)
                {
                    result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ex.Code, Description = ex.Message });
                }
                catch (Exception)
                {
                    result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.UnhandledError, Description = "An unexpected error occurred while processing this row." });
                }
            }

            result.FailureCount = result.Errors.Count;
            return result;
        }
    }

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
}
